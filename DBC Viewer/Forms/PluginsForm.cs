using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using PluginInterface;
using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace DBCViewer
{
    public partial class PluginsForm : Form
    {
        public int PluginIndex { get; private set; }
        public Assembly NewPlugin { get; private set; }

        public PluginsForm()
        {
            InitializeComponent();
        }

        public void SetPlugins(IList<IPlugin> plugins)
        {
            foreach (IPlugin plugin in plugins)
            {
                var item = string.Format(CultureInfo.InvariantCulture, "{0}", plugin.GetType().Name);
                listBox1.Items.Add(item);
            }

            listBox1.SelectedIndex = 0;
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            PluginIndex = listBox1.SelectedIndex;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var form = new Form { FormBorderStyle = FormBorderStyle.SizableToolWindow, StartPosition = FormStartPosition.CenterParent, Width = 200, Height = 200 };
            form.Controls.Add(new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                Text = Properties.Resources.pluginTemplate,
                ScrollBars = ScrollBars.Both
            });
            form.ShowDialog();

            string sourceFile = form.Controls[0].Text;

            CSharpCodeProvider provider = new CSharpCodeProvider();

            // Build the parameters for source compilation.
            CompilerParameters cp = new CompilerParameters();

            // Add an assembly reference.
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.ComponentModel.Composition.dll");
            cp.ReferencedAssemblies.Add("System.Data.dll");
            cp.ReferencedAssemblies.Add("System.Xml.dll");
            cp.ReferencedAssemblies.Add("PluginInterFace.dll");

            // Generate an executable instead of
            // a class library.
            cp.GenerateExecutable = false;

            // Set the assembly file name to generate.
            //cp.OutputAssembly = exeFile;

            // Save the assembly as a physical file.
            cp.GenerateInMemory = true;

            // Invoke compilation.
            CompilerResults cr = provider.CompileAssemblyFromSource(cp, sourceFile);

            if (cr.Errors.Count > 0)
            {
                // Display compilation errors.
                Console.WriteLine("Errors building {0} into {1}", "New Plugin", cr.PathToAssembly);
                foreach (CompilerError ce in cr.Errors)
                {
                    Console.WriteLine("  {0}", ce.ToString());
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine(string.Format("Source {0} built into {1} successfully.", "New Plugin", cr.PathToAssembly));

                NewPlugin = cr.CompiledAssembly;
            }

            // Return the results of compilation.
            if (cr.Errors.Count > 0)
            {
                PluginIndex = -1;
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else
            {
                PluginIndex = listBox1.Items.Count;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
