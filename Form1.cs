using System.Configuration;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace hlm2localization_editor
{
    public partial class Form1 : Form
    {
        public struct Language
        {
            public uint ID;
            public uint NameLength;
            public string Name;
            public uint Unk0;
            public uint EntryCount;
            public uint[] Entries;
            public string[] Strings;
        }
        public struct LocDataTable
        {
            public uint LangCount;
            public Language[] Languages;

        }
        LocDataTable LocData = new LocDataTable();
        public string LocFile;

        public Form1()
        {
            InitializeComponent();
            dataGridView1.Columns.Clear();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            var fileContent = string.Empty;
            var filePath = string.Empty;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "hlm2_localization.bin (*.bin)|*.bin";
            openFileDialog.FilterIndex = 0;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                filePath = openFileDialog.FileName; 
                LocFile = openFileDialog.FileName;

                //Read the contents of the file into a stream
                var fileStream = openFileDialog.OpenFile();

                using (var reader = new BinaryReader(fileStream))
                {
                    LocData.LangCount = reader.ReadUInt32();
                    LocData.Languages = new Language[LocData.LangCount];

                    for (int l = 0; l < LocData.LangCount; l++)
                    {
                        var Lang = new Language();

                        Lang.ID = reader.ReadUInt32();
                        Lang.NameLength = reader.ReadUInt32();
                        Lang.Name = new string(reader.ReadChars((int)Lang.NameLength));
                        Lang.Unk0 = reader.ReadUInt32();
                        Lang.EntryCount = reader.ReadUInt32();
                        Lang.Entries = new uint[Lang.EntryCount + 1];
                        Lang.Strings = new string[Lang.EntryCount];
                        for (int i = 0; i < Lang.EntryCount + 1; i++)
                        {
                            Lang.Entries[i] = reader.ReadUInt32();
                        }
                        for (int i = 0; i < Lang.EntryCount; i++)
                        {
                            // strlen()
                            string s = string.Empty;
                            char c;
                            do
                            {
                                c = reader.ReadChar();
                                s += c;
                            } while (c != '\0');
                            Lang.Strings[i] = s;
                        }
                        LocData.Languages[l] = Lang;
                    }

                }

                for (int i = 0; i < LocData.LangCount; i++)
                {
                    Language lang = LocData.Languages[i];
                    dataGridView1.Columns.Add(lang.ID.ToString(), lang.Name);
                    dataGridView1.Columns[0].ReadOnly = true;
                    dataGridView1.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }
                for (int i = 0; i < LocData.Languages[0].Strings.Length; i++)
                {
                    List<string> strs = new List<string>();
                    for (int j = 0; j < LocData.LangCount; j++)
                    {
                        strs.Add(LocData.Languages[j].Strings[i]);
                    }
                    dataGridView1.Rows.Add(strs.ToArray());
                }
            }

            //MessageBox.Show($"Lang count: {LocData.LangCount}\n{LocData.Languages[0].ID}\n{LocData.Languages[0].NameLength}\n{LocData.Languages[0].Name}\n{LocData.Languages[0].EntryCount}\n{LocData.Languages[0].Entries[0]}\n{LocData.Languages[0].Strings[0]}", "SUCCESS!", MessageBoxButtons.OK);

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
            Application.Exit();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Columns.Count == 0)
            {
                MessageBox.Show("No file.", "Error", MessageBoxButtons.OK);
                return;
            }
            SaveFile();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Columns.Count == 0)
            {
                MessageBox.Show("No file.", "Error", MessageBoxButtons.OK);
                return;
            }

            Stream stream;
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "bin files (*.bin)|*.bin";
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveFile(saveFileDialog.OpenFile());
            }

        }

        void SaveFile(Stream? stream = null)
        {
            //Applying changes
            for (int i = 0; i < LocData.Languages[0].Strings.Length; i++)
            {
                for (int j = 0; j < LocData.LangCount; j++)
                {
                    if (dataGridView1.Rows[i].Cells[j].Value == null)
                    {
                        dataGridView1.Rows[i].Cells[j].Value = "\0";
                    }
                    LocData.Languages[j].Strings[i] = dataGridView1.Rows[i].Cells[j].Value.ToString();

                    if (LocData.Languages[j].Strings[i][LocData.Languages[j].Strings[i].Length - 1] != '\0')
                    {
                        LocData.Languages[j].Strings[i] += '\0';
                    }
                }
            }


            //Saving


            //var fn = LocFile;
            //fn.Replace(".bin", "_edit.bin");
            Stream saveStream = null;
            if (stream != null)
            {
                saveStream = stream;
            }
            else
            {
                saveStream = File.Open(LocFile, FileMode.Create);
            }

            {
                using (var writer = new BinaryWriter(saveStream))
                {
                    writer.Write(LocData.LangCount);
                    for (int i = 0; i < LocData.LangCount; i++)
                    {
                        Language Lang = LocData.Languages[i];
                        writer.Write(Lang.ID);
                        writer.Write(Lang.NameLength);
                        string s = string.Empty;
                        byte[] _utf16Bytes = Encoding.Unicode.GetBytes(Lang.Name);
                        byte[] _utf8Bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, _utf16Bytes);
                        for (int b = 0; b < _utf8Bytes.Length; b++)
                        {
                            writer.Write(_utf8Bytes[b]);
                        }
                        writer.Write(Lang.Unk0);
                        writer.Write(Lang.EntryCount);
                        uint dataPos = 0;
                        writer.Write(dataPos);
                        //uint[] Entries;
                        for (int j = 0; j < Lang.Strings.Length; j++)
                        {
                            byte[] utf16Bytes = Encoding.Unicode.GetBytes(Lang.Strings[j]);
                            byte[] utf8Bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, utf16Bytes);
                            dataPos += Convert.ToUInt32(utf8Bytes.Length);
                            writer.Write(dataPos);
                        }

                        for (int j = 0; j < Lang.Strings.Length; j++)
                        {
                            byte[] utf16Bytes = Encoding.Unicode.GetBytes(Lang.Strings[j]);
                            byte[] utf8Bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, utf16Bytes);
                            for (int b = 0; b < utf8Bytes.Length; b++)
                            {
                                writer.Write(utf8Bytes[b]);
                            }
                            //writer.Write(new byte());
                        }
                    }
                }
            }
            saveStream.Close();
            //stream.Close();
            MessageBox.Show("File Saved!", "Success!", MessageBoxButtons.OK);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo("https://github.com/MaximScherbinin/hlm2localization-editor/") { UseShellExecute = true });
        }
    }
}
