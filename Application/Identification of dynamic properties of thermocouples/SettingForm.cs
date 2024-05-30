using Identification_of_dynamic_properties_of_thermocouples.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Identification_of_dynamic_properties_of_thermocouples
{   
    public partial class SettingForm : Form
    {
        public Form1 Form1Instance { get; set; }
        public SettingForm()
        {
            InitializeComponent();
            comboBox1.Items.Add(Settings.Default.COMIN);
            comboBox2.Items.Add(Settings.Default.COMOUT);
            comboBox1.SelectedIndex = comboBox2.SelectedIndex = 0;
            textBox1.Text = Settings.Default.SavePath.ToString();
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            DialogResult = folderBrowserDialog1.ShowDialog();
            if (DialogResult == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
                Settings.Default.SavePath = textBox1.Text;
                SaveSettings();
            }
        }
        public void SaveSettings() 
        {
            Settings.Default.Save();
            Settings.Default.Reload();
            GC.Collect(); 
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            textBox1.Text = Settings.Default.SavePath;
            textBox2.Text = Settings.Default.maxTemperature.ToString();
            textBox3.Text = Settings.Default.desiredTemperature.ToString();
            textBox4.Text = Settings.Default.Resistance.ToString();
            if (Settings.Default.operatingMode == 1001)
            { 
                R1001.Select();
            }
            else R1002.Select();
            
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.Text == "") textBox2.Text = "0";
            try
            {
                if (int.Parse(textBox3.Text) >= int.Parse(textBox2.Text))
                {
                    label6.Text = "Desired temprature cannot be\nhigher than maximal temperature";
                    textBox3.Text = textBox2.Text;
                }
                else if ((int.Parse(textBox3.Text) < 20) || (int.Parse(textBox3.Text) > 150))
                {
                    label6.Text = "Desired temperature might not be reached";
                }
                else
                {
                    label6.Text = "";
                }
                Settings.Default.maxTemperature = short.Parse(textBox2.Text);
                SaveSettings();
            }
            catch 
            {
               // textBox2.Text = "";
            }
            if (Settings.Default.maxTemperature.ToString() != textBox2.Text) label10.Visible = true;
        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (Settings.Default.desiredTemperature.ToString() != textBox3.Text) label10.Visible = true;

            if (textBox3.Text == "")
            {
                textBox3.Text = "0";
                textBox3.SelectAll();
            }
            try
            {   
                if (int.Parse(textBox3.Text) >= int.Parse(textBox2.Text))
                {
                    label6.Text = "Desired temprature cannot be\nhigher than maximal temperature";
                    textBox3.Text = textBox2.Text;
                }
                else if ((int.Parse(textBox3.Text) < 20) || (int.Parse(textBox3.Text) > 150))
                {
                    label6.Text = "Desired temperature might not be reached";
                }
                else
                {
                    label6.Text = "";
                }
                Settings.Default.desiredTemperature = short.Parse(textBox3.Text);
                SaveSettings();
            }
            catch
            {
                textBox3.Text = ""; 
            }
           
        }
        private void ChangeOperatingMode(object sender, EventArgs e)
        {   
            if (Form1Instance.serialPort != null && Form1Instance.serialPort.IsOpen == true)
            {
                RadioButton radioButton = sender as RadioButton;
                if (Settings.Default.operatingMode != short.Parse(radioButton.Name.Trim('R')))
                {
                    Settings.Default.operatingMode = short.Parse(radioButton.Name.Trim('R'));
                    Form1Instance.SendSettingsCommand(Settings.Default.operatingMode.ToString());
                    SaveSettings();
                }
            }
            else
            {
                MessageBox.Show("Device is not connected");
            }
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e) //change maximal temperature
        {
            if (e.KeyChar == (char)Keys.Enter) 
            {
                int MaxTemperature = Settings.Default.maxTemperature + 10000;
                Form1Instance.SendSettingsCommand(MaxTemperature.ToString());
                e.Handled = true;
            }
            label10.Visible = false;
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e) //change desired temperature
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                int DesiredTemperature = Settings.Default.desiredTemperature + 11000;
                Form1Instance.SendSettingsCommand(DesiredTemperature.ToString());
                e.Handled = true;
            }
            label10.Visible = false;
        }

        #region COM PORTS
        private void button1_Click(object sender, EventArgs e) //search for comports and write them to combobox
        {
            string[] ports = SerialPort.GetPortNames();
            comboBox1.Items.Clear();
            comboBox2.Items.Clear();

            foreach (string port in ports)
            {
                comboBox1.Items.Add(port); 
                comboBox2.Items.Add(port);
            }

            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
                comboBox2.SelectedIndex = 0;
            }
            if (comboBox1.Items.Count > 1)
            {
                comboBox2.SelectedIndex = 1;
            }
            else
            {
                comboBox1.Items.Add("No ports available"); 
                comboBox1.SelectedIndex = 0;
                comboBox1.Enabled = false; 
            }
        }

        private void SaveCOMPorts()
        {
            Settings.Default.COMIN = comboBox1.Text.ToString();
            Settings.Default.COMOUT = comboBox2.Text.ToString();
            Console.WriteLine("Saved: " + Settings.Default.COMIN + " " + Settings.Default.COMOUT);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SaveCOMPorts();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            SaveCOMPorts();
        }

        #endregion

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.Resistance = textBox4.Text;
            Console.WriteLine(Settings.Default.Resistance);
        }

        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
            (e.KeyChar != ','))
            {
                e.Handled = true;
            }

            // only allow one decimal point
            if ((e.KeyChar == ',') && ((sender as System.Windows.Forms.TextBox).Text.IndexOf(',') > -1))
            {
                e.Handled = true;
            }
        }
    }
}
