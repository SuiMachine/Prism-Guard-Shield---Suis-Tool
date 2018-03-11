using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace PrismGuardShield_Tool
{
    public partial class Form1 : Form
    {
        static string _GamesExecutable = "PRISM.exe";
        static string _GamesExecutableBackup = "PRISM.bak";
        static string PCGW_URL = "http://pcgamingwiki.com/";
        static string donationURL = "https://www.twitchalerts.com/donate/suicidemachine";
        bool VerticalMinus;

        int width = 1280;
        int height = 720;
        float fov = 90;
        float aspectRatio = 1.777777f;

        byte[] FovAsArray = new byte[4];
        byte[] AspectRatioAsArray = new byte[4];

        byte[] data;
        int fovAdress = 0x002B7EBC;
        int aspectRatioAdress = 0x003CE484;
        int aspectRatioAdress2 = 0x00038DC9;
        int aspectRatioAdress3 = 0x0003AD63;


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if(!File.Exists(_GamesExecutable))
            {
                MessageBox.Show("No executable found. Please place the file in a folder with a game.");
                Close();
            }
            else
            {
                if(!CheckForCorrectFile())
                {
                    MessageBox.Show("Wrong version of the file. Check WSGF!");
                    Close();
                }
                else
                {
                    data = GetBytesFromAFile(_GamesExecutable);
                    string s = BitConverter.ToString(data);

                    fov = ConvertToDegrees(BitConverter.ToSingle(data, fovAdress));
                    aspectRatio = BitConverter.ToSingle(data, aspectRatioAdress);

                    TB_FOV.Text = fov.ToString();
                    TB_ResX.Text = width.ToString();
                    height = (int)(width / aspectRatio);
                    TB_ResY.Text = height.ToString();
                    GetArray();
                }
            }
        }

        private bool CheckForCorrectFile()
        {
            FileInfo inf = new FileInfo(_GamesExecutable);
            if(inf.Length != 4022272)
                return false;

            var ver = FileVersionInfo.GetVersionInfo(_GamesExecutable);
            string verSafe = ver.FileVersion.Replace(".", ","); //not sure if this is regional or not, so I replace it for safety
            if(verSafe == "1, 1, 2, 5")
                return true;
            else
                return false;
        }

        private void CB_HorizontalPlusFOV_CheckedChanged(object sender, EventArgs e)
        {
            this.VerticalMinus = !((CheckBox)sender).Checked;
        }

        #region Buttons_Lables_and_Stuff
        private void TB_ResX_TextChanged(object sender, EventArgs e)
        {
            if(int.TryParse(TB_ResX.Text, out int temp))
            {
                width = temp;
            }
            else
                width = 1280;
        }

        private void TB_ResY_TextChanged(object sender, EventArgs e)
        {
            if(int.TryParse(TB_ResY.Text, out int temp))
            {
                height = temp;
            }
            else
                height = 720;
        }

        private void GetArray()
        {
            if(height > 0)
                aspectRatio = width * 1.0f / height;

            if(!VerticalMinus)
            {
                float radian = ConvertToHorzontalPlusFOV(fov);
                FovAsArray = BitConverter.GetBytes(radian);

            }
            else
            {
                float radian = ConvertToRadians(fov);
                FovAsArray = BitConverter.GetBytes(radian);

            }

            AspectRatioAsArray = BitConverter.GetBytes(aspectRatio);
        }
        #endregion

        #region Load_Save
        private byte[] GetBytesFromAFile(string filename)
        {
            FileStream fs = null;
            try
            {
                fs = File.OpenRead(filename);
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                return bytes;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
        }

        private bool WriteBytesToAFile(string filename, byte[] usedData)
        {
            try
            {
                File.WriteAllBytes(filename, usedData);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return false;
            }
        }

        private void ReplaceInArray(byte[] array, int offset, byte[] replacingBytes, int lenght = 4)
        {
            for(int i=0; i<lenght; i++)
            {
                array[offset + i] = replacingBytes[i];
            }
        }

        private void B_WriteToAFile_Click(object sender, EventArgs e)
        {
            GetArray();

            ReplaceInArray(data, fovAdress, FovAsArray);
            ReplaceInArray(data, aspectRatioAdress, AspectRatioAsArray);
            ReplaceInArray(data, aspectRatioAdress2, AspectRatioAsArray);
            ReplaceInArray(data, aspectRatioAdress3, AspectRatioAsArray);



            //Backup
            if(!File.Exists(_GamesExecutableBackup))
            {
                File.Copy(_GamesExecutable, _GamesExecutableBackup);
            }

            bool success = true;
            success = WriteBytesToAFile(_GamesExecutable, data);

            if (!success)
                MessageBox.Show("There was an error writting to a file!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                MessageBox.Show("Successfully made the changes!", "Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        private void LL_PCGW_link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(PCGW_URL);
        }

        private void DonateLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(donationURL);
        }

        private void TB_FOV_Changed(object sender, EventArgs e)
        {
            if(float.TryParse(TB_FOV.Text, out float parsedFOV))
            {
                if(parsedFOV > 45f && parsedFOV < 179f)
                {
                    fov = parsedFOV;
                }
                else
                    fov = 90f;
            }
            else
            {
                fov = 90f;
            }
        }

        #region Calculator_Functions
        public float ConvertToDegrees(float angle)
        {
            return (float)(angle * (180.0 / Math.PI));
        }

        private float ConvertToRadians(float angle)
        {
            return (float)(Math.PI * angle / 180.0);
        }

        public float ConvertToHorzontalPlusFOV(float angle)
        {
            if(width == 0 || height == 0)
            {
                return 0f;
            }

            // Horizontal FOV to Horizontal Radians
            var HorizontalRadians = ConvertToRadians(angle);

            // Horizontal Radians to Vertical Radians
            var VerticalRadians = 2 * Math.Atan(Math.Tan(HorizontalRadians / 2) * (3 * 1.0 / 4 * 1.0));

            // Vertical Radian to Horizontal Radian (with aspect ratio)
            var HorizontalRadianPlus = 2 * Math.Atan(Math.Tan(VerticalRadians / 2) * (width * 1.0 / height * 1.0));

            return (float)(HorizontalRadianPlus);
        }
        #endregion
    }
}
