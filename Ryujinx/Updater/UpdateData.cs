/* =========================================
 *  This service is responsible for parsing
 *  the appveyor json.
 *
 *  =========================================
 *
 *  Strings and other variables: These are stored for the config page.
 *
 *  JobID:          String, stores the parsed JobID.
 *  BuildVer:       String, stores the parsed BuildVersion.
 *  BuildURL:       String, stores the BuildURL.
 *  BuildArt:       String, stores the parsed build artifact (URL).
 *  BuildCommit:    String, stores the parsed build commit; and is stored in a five character substring.
 *  Branch:         String, stores the parsed branch for the build.
 * =========================================
 */

namespace Ryujinx.Updater
{
    public struct UpdateData
    {
        public string JobID          { get; set; }
        public string BuildVer       { get; set; }
        public string BuildURL       { get; set; }
        public string BuildArt       { get; set; }
        public string BuildCommit    { get; set; }
        public string Branch         { get; set; }
    }
}