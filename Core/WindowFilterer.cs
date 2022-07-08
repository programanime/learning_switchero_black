/*
 * Switcheroo - The incremental-search task switcher for Windows.
 * http://www.switcheroo.io/
 * Copyright 2009, 2010 James Sulak
 * Copyright 2014 Regin Larsen
 * 
 * Switcheroo is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Switcheroo is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Switcheroo.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Switcheroo.Core.Matchers;
using System.Web.Script.Serialization;
using System.IO;

namespace Switcheroo.Core
{
    public static class Prompt
    {
        private static string filename = Path.GetTempPath()+"\\switch.txt";
        public static Dictionary<string, string> dict = new Dictionary<string, string>();
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 35,
                FormBorderStyle = FormBorderStyle.None,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };

            System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox() { Left = 10, Top = 10, Width = 480, Height = 35 };

            System.Windows.Forms.Button confirmation = new System.Windows.Forms.Button() { Text = "Ok", Left = 350, Width = 0, Top = 70, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;
            //prompt.Enter += (sender, e) => { prompt.Close(); };
            prompt.BringToFront();
            prompt.ShowDialog();
            prompt.BringToFront();
            //Thread.Sleep(500);
            return textBox.Text;
        }

        public static string getWindowTitle(string title)
        {
            if (!File.Exists(filename)) return title;
            string text = File.ReadAllText(filename);
            var lines = text.Split('\n');
            foreach(var line in lines)
            {
                if (line.StartsWith(title + "~"))
                {
                    return line.Split('~')[1];
                }
            }
            return title;
        }

        public static void setWindowTitle(string currenTitle, string newTitle)
        {
            var text = currenTitle + "~" + newTitle;
            if (File.Exists(filename))
            {
                text += "\n" + File.ReadAllText(filename);
            }

            File.WriteAllText(filename, text);
        }
    }

    public class WindowFilterer
    {
        public IEnumerable<FilterResult<T>> Filter<T>(WindowFilterContext<T> context, string query) where T : IWindowText
        {
            var filterText = query;
            string processFilterText = null;

            var queryParts = query.Split(new [] {'.'}, 2);

            if (queryParts.Length == 2)
            {
                processFilterText = queryParts[0];
                if (processFilterText.Length == 0)
                {
                    processFilterText = context.ForegroundWindowProcessTitle;
                }

                filterText = queryParts[1];
            }

            return context.Windows
                .Select(
                    w =>
                        new
                        {
                            Window = w,
                            ResultsTitle = Score(Prompt.getWindowTitle(w.WindowTitle), filterText),
                            ResultsProcessTitle = Score(w.ProcessTitle, processFilterText ?? filterText)
                        })
                .Where(r =>
                {
                    if (processFilterText == null)
                    {
                        return r.ResultsTitle.Any(wt => wt.Matched) || r.ResultsProcessTitle.Any(pt => pt.Matched);
                    }
                    return r.ResultsTitle.Any(wt => wt.Matched) && r.ResultsProcessTitle.Any(pt => pt.Matched);
                })
                .OrderByDescending(r => r.ResultsTitle.Sum(wt => wt.Score) + r.ResultsProcessTitle.Sum(pt => pt.Score))
                .Select(
                    r =>
                        new FilterResult<T>
                        {
                            AppWindow = r.Window,
                            WindowTitleMatchResults = r.ResultsTitle,
                            ProcessTitleMatchResults = r.ResultsProcessTitle
                        });
        }

        private static List<MatchResult> Score(string title, string filterText)
        {
            var startsWithMatcher = new StartsWithMatcher();
            var containsMatcher = new ContainsMatcher();
            var significantCharactersMatcher = new SignificantCharactersMatcher();
            var individualCharactersMatcher = new IndividualCharactersMatcher();

            var results = new List<MatchResult>
            {
                startsWithMatcher.Evaluate(title, filterText),
                significantCharactersMatcher.Evaluate(title, filterText),
                containsMatcher.Evaluate(title, filterText),
                individualCharactersMatcher.Evaluate(title, filterText)
            };

            return results;
        }
    }

    public class WindowFilterContext<T> where T : IWindowText
    {
        public string ForegroundWindowProcessTitle { get; set; }
        public IEnumerable<T> Windows { get; set; } 
    }
}