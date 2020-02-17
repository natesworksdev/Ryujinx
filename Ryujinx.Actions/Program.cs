using Octokit;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ryujinx.Actions
{
    class Program
    {
        private const string GITHUB_USER_CONTENT_BASE_URL = "https://raw.githubusercontent.com/";
        private const string GITHUB_COMMIT_BASE_URL = "https://github.com/Ryujinx/Ryujinx/commit/";

        private const string TARGET_JSON_NAME = "latest.json";

        static void Main(string[] args)
        {
            string operation = Core.GetInput("operation");

            if (operation == null)
            {
                Core.SetFailed("operation must be specified!");
            }
            else
            {
                string releaseRepository;

                switch (operation)
                {
                    case "get-build-version":
                        releaseRepository = Core.GetInput("release_repository");

                        string releaseInformationUrl = GITHUB_USER_CONTENT_BASE_URL + releaseRepository + "/master/" + TARGET_JSON_NAME;

                        WebClient webClient = new WebClient();

                        string latestVersion = webClient.DownloadString(releaseInformationUrl);

                        ReleaseInformation releaseInformation = JsonSerializer.Deserialize<ReleaseInformation>(latestVersion, CreateJsonOptions());

                        releaseInformation.Version.IncrementPatch();

                        Core.SetOutput("release-version", releaseInformation.Version.ToString());
                        break;
                    case "publish-release":
                        string githubWorkspace   = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
                        string artifactDirectory = Core.GetInput("artifacts_directory");
                        string releaseToken      = Core.GetInput("release_token");

                        releaseRepository = Core.GetInput("release_repository");

                        // Mark the token as secret
                        Core.SetSecret(releaseToken);

                        if (artifactDirectory == null)
                        {
                            artifactDirectory = "artifacts";
                        }

                        string artifactFullPath = Path.Join(githubWorkspace, artifactDirectory);
                        string releaseVersion   = Core.GetInput("release_version");

                        PublishRelease(artifactFullPath, VersionCore.FromString(releaseVersion), releaseRepository, releaseToken).Wait();
                        break;
                    default:
                        Core.SetFailed($"Unknown operation {operation} must be specified!");
                        break;
                }
            }
        }

        private static GitHubClient CreateGitHubClientInstance(string releaseToken)
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("Ryujinx.Actions", "1.0.0"));

            client.Credentials = new Credentials(releaseToken);

            return client;
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            JsonSerializerOptions serializeOptions = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            serializeOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            serializeOptions.Converters.Add(new VersionCore.Converter());

            return serializeOptions;
        }

        private static Task<ReleaseAsset> UploadAsset(GitHubClient client, Release release, Stream stream, string fileName, string contentType)
        {
            Core.Info($"Uploading {fileName}");

            ReleaseAssetUpload assetUpload = new ReleaseAssetUpload()
            {
                FileName    = fileName,
                ContentType = contentType,
                RawData     = stream,
                Timeout     = TimeSpan.FromMinutes(30)
            };

            return client.Repository.Release.UploadAsset(release, assetUpload);
        }

        private static async Task UploadArtifact(GitHubClient client, Release release, string artifactDirectory, ArtifactInformation artifactInformation)
        {
            using (FileStream fileStream = File.OpenRead(Path.Combine(artifactDirectory, artifactInformation.FileName)))
            {
                await UploadAsset(client, release, fileStream, artifactInformation.FileName, "application/zip");
            }
        }

        private static async Task PublishRelease(string artifactDirectory, VersionCore releaseVersion, string releaseRepository, string releaseToken)
        {
            if (releaseVersion == null)
            {
                Core.SetFailed("Invalid release_version!");
                return;
            }

            if (releaseToken == null)
            {
                Core.SetFailed("Invalid release_token!");
                return;
            }

            if (releaseRepository == null)
            {
                Core.SetFailed("Invalid release_repository!");
                return;
            }

            string commitSha = Environment.GetEnvironmentVariable("GITHUB_SHA");

            if (commitSha == null)
            {
                Core.SetFailed("GITHUB_SHA environment variable not found!");
                return;
            }

            string[] releaseRepositoryParts = releaseRepository.Split("/");

            if (releaseRepositoryParts.Length != 2)
            {
                Core.SetFailed("Invalid release_repository!");
                return;
            }

            string releaseRepositoryOwner = releaseRepositoryParts[0];
            string releaseRepositoryName  = releaseRepositoryParts[1];

            ReleaseInformation releaseInformation = new ReleaseInformation(releaseVersion);

            foreach (string file in Directory.GetFiles(artifactDirectory, $"ryujinx*-{releaseVersion}-*.zip"))
            {
                ArtifactInformation artifactInformation = ArtifactInformation.FromFile(file);

                releaseInformation.Artifacts.Add(artifactInformation);
            }

            GitHubClient client = CreateGitHubClientInstance(releaseToken);

            // Create a new release
            NewRelease newRelease = new NewRelease($"build-{commitSha}");
            newRelease.Name       = releaseVersion.ToString();
            newRelease.Body       = $"Triggered by {GITHUB_COMMIT_BASE_URL}{commitSha}.";
            newRelease.Draft      = true;
            newRelease.Prerelease = false;

            Release release             = await client.Repository.Release.Create(releaseRepositoryOwner, releaseRepositoryName, newRelease);
            ReleaseUpdate updateRelease = release.ToUpdate();

            // Upload artifacts
            foreach (ArtifactInformation information in releaseInformation.Artifacts)
            {
                await UploadArtifact(client, release, artifactDirectory, information);
            }

            string releaseInformationJson = JsonSerializer.Serialize(releaseInformation, CreateJsonOptions());

            // Upload release information
            using (MemoryStream releaseInformationStream = new MemoryStream(Encoding.UTF8.GetBytes(releaseInformationJson)))
            {
                await UploadAsset(client, release, releaseInformationStream, "release_information.json", "application/json");
            }

            updateRelease.Draft = false;

            release = await client.Repository.Release.Edit(releaseRepositoryOwner, releaseRepositoryName, release.Id, updateRelease);

            Core.Info($"Successfully published release {releaseVersion} to {release.HtmlUrl}");
        }
    }
}
