using System.Text;
using System.Collections.Generic;

namespace WSE2_CLI_Options
{
    public class CLI_Options
    {
        public bool IntroDisabled;
        public string Module;
        public string ConfigPath;
        public List<string> AdditionalArgs = new List<string>();

        public string RenderOptions()
        {
            return this.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (IntroDisabled)
                sb.Append(" --no-intro");
            sb.Append(" --module ");
            sb.Append(Module);

            if (!(ConfigPath is null))
            {
                sb.Append(" --config-path ");
                sb.Append(ConfigPath);
            }

            foreach (var arg in AdditionalArgs)
            {

                sb.Append(" ");
                sb.Append(arg);
            }
            return sb.ToString();
        }
    }
}
