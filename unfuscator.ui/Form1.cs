using System;
using System.Linq;
using System.Windows.Forms;
using Unfuscator.Core;

namespace Unfuscator.Gui
{
    public partial class Form1 : Form
    {
        private UnObfuscator unfuscator;

        public Form1()
        {
            InitializeComponent();
        }

        private void Changed()
        {
            if (unfuscator == null)
                return;
            try
            {
                var unfuscated = unfuscator.Unfuscate(obfuscatedTextBox.Text, ((VersionItem)versionComboBox.SelectedItem).Version);

                unobfuscatedTextBox.Text = unfuscated.ToString();
            }
            catch (Exception e)
            {
                unobfuscatedTextBox.Text = e.ToString();
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*"
            };
            if (open.ShowDialog() != DialogResult.OK)
                return;

            var mapping = Mapping.Empty();

            mapping.LoadDotfuscator(open.FileNames, (path, ex) => MessageBox.Show($"Failed to load {path}: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error), (path, progress) => { });

            unfuscator = new UnObfuscator(mapping);
            obfuscatedTextBox.TextChanged += (s, a) => Changed();
            foreach (var ver in mapping.Versions)
            {
                versionComboBox.Items.Add(new VersionItem(ver));
            }
            versionComboBox.SelectedItem = new VersionItem(mapping.Versions.FirstOrDefault());
        }

        private class VersionItem
        {
            public Version Version { get; }

            public VersionItem(Version v)
            {
                Version = v;
            }

            public override string ToString()
            {
                return Version == null ? "No version" : "v" + Version;
            }

            protected bool Equals(VersionItem other)
            {
                return Equals(Version, other.Version);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((VersionItem)obj);
            }

            public override int GetHashCode()
            {
                return (Version != null ? Version.GetHashCode() : 0);
            }
        }


    }
}
