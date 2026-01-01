using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.XPath;
using static System.Net.Mime.MediaTypeNames;

namespace PlantUMLEncoder
{
    public partial class Encoder : Form
    {
        private Button encodeBtn;

        public Encoder()
        {
            InitializeComponent();
            this.Resize += new EventHandler(Form1_resize);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            encodeBtn = new Button()
            {
                Location = new Point(0, 0),
                Height = 75,
                Visible = true,
                Width = this.Width,
                Text = "Кодировать\n данные\nиз буфера\nи записать в файл",
                BackColor = Color.LightPink
            };

            encodeBtn.Click += EncodeBtn_Click;
            Controls.Add(encodeBtn);

            this.Height = encodeBtn.Top + encodeBtn.Height + 38;
        }

        private void Form1_resize(object sender, EventArgs e)
        {
            if (encodeBtn != null)
            {
                encodeBtn.Width = this.Width;
                encodeBtn.Height = this.Height;
            }
        }

        private void EncodeBtn_Click(object sender, EventArgs e)
        {
            string data = null;
            IDataObject dataObject = Clipboard.GetDataObject();

            if (dataObject.GetDataPresent(DataFormats.Text))
            {
                data = Clipboard.GetText(TextDataFormat.Text);
                string[] rows = data.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (rows.Any() && rows.First().Trim() == "@startuml")
                {
                    string u = null;
                    PlantUMLEncoder.PlantUmlTextEncoder encoder = new PlantUmlTextEncoder();
                    using (StringReader reader = new StringReader(data))
                    {
                        u = "# **Диаграмма последовательности:**<br>\r\n![](//www.plantuml.com/plantuml/png/"+encoder.Encode(reader)+")";
                    }

                    using (SaveFileDialog dialog = new SaveFileDialog())
                    {
                        dialog.Title = "Выберите файл для записи";
                        DialogResult result = dialog.ShowDialog();

                        if (result == DialogResult.OK)
                        {
                            using (StreamWriter writer = new StreamWriter(dialog.FileName))
                            {
                                writer.WriteLine(u);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Это НЕ Plantuml код");
                    return;
                }
            }     
        }
    }
    public class PlantUmlTextEncoder
    {
        public string Encode(TextReader reader)
        {
            using (var output = new MemoryStream())
            {
                using (var writer = new StreamWriter(new DeflateStream(output, CompressionLevel.Optimal), Encoding.UTF8))
                    writer.Write(reader.ReadToEnd());
                return Encode(output.ToArray());
            }
        }

        private static string Encode(IReadOnlyList<byte> bytes)
        {
            var length = bytes.Count;
            var s = new StringBuilder();
            for (var i = 0; i < length; i += 3)
            {
                var b1 = bytes[i];
                var b2 = i + 1 < length ? bytes[i + 1] : (byte)0;
                var b3 = i + 2 < length ? bytes[i + 2] : (byte)0;
                s.Append(Append3Bytes(b1, b2, b3));
            }
            return s.ToString();
        }

        private static char[] Append3Bytes(byte b1, byte b2, byte b3)
        {
            var c1 = b1 >> 2;
            var c2 = (b1 & 0x3) << 4 | b2 >> 4;
            var c3 = (b2 & 0xF) << 2 | b3 >> 6;
            var c4 = b3 & 0x3F;
            return new[]
            {
            EncodeByte((byte) (c1 & 0x3F)),
            EncodeByte((byte) (c2 & 0x3F)),
            EncodeByte((byte) (c3 & 0x3F)),
            EncodeByte((byte) (c4 & 0x3F))
         };
        }

        private static char EncodeByte(byte b)
        {
            var ascii = Encoding.ASCII;
            if (b < 10)
                return ascii.GetChars(new[] { (byte)(48 + b) })[0];
            b -= 10;
            if (b < 26)
                return ascii.GetChars(new[] { (byte)(65 + b) })[0];
            b -= 26;
            if (b < 26)
                return ascii.GetChars(new[] { (byte)(97 + b) })[0];
            b -= 26;
            if (b == 0)
                return '-';
            if (b == 1)
                return '_';
            return '?';
        }
    }
}
