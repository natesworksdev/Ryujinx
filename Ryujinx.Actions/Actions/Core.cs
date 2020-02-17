using System;
using System.Collections.Generic;

namespace Ryujinx.Actions
{
    public class Core
    {
        private const string COMMAND_PREFIX = "::";

        private static string EscapeBase(string data)
        {
            return data.Replace("%", "%25")
                       .Replace("\r", "%0D")
                       .Replace("\n", "%0A");
        }

        private static string EscapeMessage(string message)
        {
            return EscapeBase(message).Replace(":", "%3A").Replace(",", "%2C");
        }

        private static string ComputeString(string command, IDictionary<string, string> properties, string message)
        {
            string result = $"{COMMAND_PREFIX}{command}";

            if (properties != null && properties.Count > 0)
            {
                result += " ";

                bool isFirstProperty = true;

                foreach (KeyValuePair<string, string> property in properties)
                {
                    if (!isFirstProperty)
                    {
                        result += ",";
                        isFirstProperty = false;
                    }

                    result += $"{property.Key}={EscapeBase(property.Value)}";
                }
            }

            result += $"{COMMAND_PREFIX}{EscapeMessage(message)}";

            return result;
        }

        public static void Issue(string command, IDictionary<string, string> properties = null, string message = "")
        {
            Console.Out.WriteLine(ComputeString(command, properties, message));
        }

        public static void ExportVariable(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value);

            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("name", name);

            Issue("set-env", properties, value);
        }

        public static void SetSecret(string secret)
        {
            Issue("add-mask", message: secret);
        }

        public static void AddPath(string path)
        {
            Issue("add-path", message: path);

            string systemPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);

            Environment.SetEnvironmentVariable("Path", $"{path};{systemPath}");
        }

        public static string GetInput(string name)
        {
            string result = Environment.GetEnvironmentVariable($"INPUT_{name.Replace(" ", "_")}".ToUpper());

            if (result != null)
            {
                return result.Trim();
            }

            return null;
        }

        public static void SetOutput(string name, string value)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("name", name);

            Issue("set-output", properties, value);
        }
        
        public static void SetFailed(string message)
        {
            Environment.ExitCode = 1;

            Error(message);
        }

        public static void Debug(string message)
        {
            Issue("debug", message: message);
        }

        public static void Error(string message)
        {
            Issue("error", message: message);
        }

        public static void Warning(string message)
        {
            Issue("warning", message: message);
        }

        public static void Info(string message)
        {
            Console.Out.WriteLine(message);
        }

        public static void StartGroup(string name)
        {
            Issue("group", message: name);
        }

        public static void EndGroup()
        {
            Issue("endgroup");
        }

        public static void SaveState(string name, string value)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("name", name);

            Issue("save-state", properties, value);
        }

        public static string GetState(string name)
        {
            return Environment.GetEnvironmentVariable($"STATE_{name}");
        }
    }
}
