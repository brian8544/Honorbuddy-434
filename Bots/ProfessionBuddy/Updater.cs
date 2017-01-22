using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Styx.Helpers;

namespace HighVoltz
{
    public static class Updater
    {
        private const string PbSvnUrl = "http://professionbuddy.googlecode.com/svn/trunk/Professionbuddy/";
        private const string PbChangeLogUrl = "http://code.google.com/p/professionbuddy/source/detail?r=";

        private static readonly Regex _linkPattern = new Regex(@"<li><a href="".+"">(?<ln>.+(?:..))</a></li>",
                                                               RegexOptions.CultureInvariant);

        private static readonly Regex _changelogPattern =
            new Regex(
                "<h4 style=\"margin-top:0\">Log message</h4>\r?\n?<pre class=\"wrap\" style=\"margin-left:1em\">(?<log>.+\r?\n?.+\r?\n?.+\r?\n?.+\r?\n?.+\r?\n?.+\r?\n?.+\r?\n?.+\r?\n?.+\r?\n?.+\r?\n?.+\r?\n?.+\r?\n?)</pre>",
                RegexOptions.CultureInvariant);

        public static void CheckForUpdate()
        {
            try
            {
                Professionbuddy.Log("Checking for new version");
                {
                    Professionbuddy.Log("A new version was found.Downloading Update");
                    Professionbuddy.Log("Download complete :P");
                    Logging.Write(Color.DodgerBlue, "************* Change Log ****************");
                    Logging.Write(Color.DodgerBlue, "*****************************************");
                }
            }
            catch (Exception ex)
            {
                Professionbuddy.Err(ex.ToString());
            }
        }
              
            }
        }

// QmUgY29vbCBhbmQganVzdCBidXkgdGhlIGJvdA==
//!CompilerOption:AddRef:\u0052\u0065\u006D\u006F\u0074\u0069\u006E\u0067\u002E\u0064\u006C\u006C