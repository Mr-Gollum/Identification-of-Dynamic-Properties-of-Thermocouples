using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Collections;
using System.Net.Mail;
using System.Net.Sockets;
using System.Windows.Forms.DataVisualization.Charting;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Identification_of_dynamic_properties_of_thermocouples.Properties;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace Identification_of_dynamic_properties_of_thermocouples
{
    
    public partial class Form1 : Form
    {
        public SerialPort serialPort, serialPort2;
        private SettingForm settingForm;
        int TimerSendSettings, numOfMeasurement=0, timeOfMeasurement=1;
        float energy,averageTempDifference=0,averageHot,averageCold,coldSide;
        public Form1()
        {
            InitializeComponent();
        }

        #region Load/close
        private void Form1_Load(object sender, EventArgs e)
        {
            CheckStatusOfDevice();
            B1000.Enabled = false;
            labelWarning.Text = "";
        }
        
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            serialPort.Close();
            serialPort2.Close();
        }
        #endregion
        private void button1_Click(object sender, EventArgs e) //Connect / disconnect
        {

            Application.UseWaitCursor = true;
            Application.EnableVisualStyles(); 
            if (serialPort != null && serialPort.IsOpen) //disconnect
            {
                DialogResult dialogResult = MessageBox.Show("Disconnect?", "You are connected to the device!", MessageBoxButtons.OKCancel);
                if (dialogResult == DialogResult.OK)
                {
                    serialPort.Close();
                    serialPort2.Close();
                    progressBar1.Value = 0;
                    B1000.Enabled= false;
                }
                else
                {
                    
                }
            }
            else 
            {
                progressBar1.Value = 10;
                serialPort = new SerialPort(Settings.Default.COMOUT, 115200);
                serialPort2 = new SerialPort(Settings.Default.COMIN, 115200);
                serialPort.DataReceived += SerialPort_DataReceived;
                // Open the serial port
                progressBar1.Value = 20;
                try
                {
                    progressBar1.Value = 70;
                    serialPort.Open();
                    serialPort2.Open();
                }
                catch (Exception ex)
                {
                    progressBar1.Value = 70;
                    MessageBox.Show("Failed to open serial port: " + ex.Message);
                }
                progressBar1.Value = 100;
                timer1.Enabled= true;
                timer2.Enabled= true;

            }
            CheckStatusOfDevice();
            Application.UseWaitCursor = false;
            Console.WriteLine("serial port:" + serialPort + "   port open:" + serialPort.IsOpen);
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = serialPort.ReadExisting();
            string trimmedString;

            BeginInvoke(new Action(() =>
            {
                
                int index = data.IndexOf('Z');

                if (index != -1)
                {
                    data = data.Substring(0, index);
                    trimmedString = data;
                    trimmedString = trimmedString.Replace("Z","");
                    trimmedString = trimmedString.Replace('.', ',');
                }
                else
                {
                    trimmedString = "";
                }
                
                string[] splitted = trimmedString.Split('X');

                richTextBox1.Text += data + "\n";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
                try
                {   
                    
                    float receivedVoltage = float.Parse(splitted[0]);
                    float receivedTempHot = float.Parse(splitted[1]);
                    float receivedTempCold = float.Parse(splitted[2]);
                    int receivedTempDesired = int.Parse(splitted[3]);
                    int receivedTempMax = int.Parse(splitted[4]);
                    dataReceived(receivedVoltage, receivedTempHot, receivedTempCold, receivedTempDesired, receivedTempMax);
                    visualTemperatureChart(receivedTempHot);

                    label1.Text = "Voltage: " + receivedVoltage + "mV";
                    label2.Text = "Hot side: " + receivedTempHot + "°C";
                    label3.Text = "Cold side: " + receivedTempCold + "°C";
                    label6.Text = "Temperature difference: " + (receivedTempHot - receivedTempCold) + "°C";
                    if ((receivedVoltage < 0) && (0 < (receivedTempHot - receivedTempCold))) labelWarning.Text = "Flip thermoelectric module or reverse the polarity of the wires";
                    else labelWarning.Text = "";

                    averageTempDifference += (receivedTempHot - receivedTempCold) ;
                    label10.Text = "Avg. temp. difference: " + averageTempDifference / timeOfMeasurement + "°C";
                    averageHot += receivedTempHot;
                    averageCold += receivedTempCold;

                    label11.Text = "Avg. Hot / Cold side: " + (averageHot / timeOfMeasurement) + "//" + (averageCold / timeOfMeasurement) + "°C";

                    Console.WriteLine(averageTempDifference + " +" + (receivedTempHot-receivedTempCold) + "/" + timeOfMeasurement );
                    Calculate(receivedVoltage);
                    coldSide = receivedTempCold;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

            }));
            
        }

        private void button4_Click(object sender, EventArgs e) //export chart
        {
            DialogResult result;
            result = MessageBox.Show("Do you want too keep data in charts?", "Data export", MessageBoxButtons.YesNoCancel);

            if (result == System.Windows.Forms.DialogResult.Yes) //export and keep
            {
                ExportData();
            }
            else if (result == System.Windows.Forms.DialogResult.No) //export and delete
            {
                ExportData();
                DeleteData();
            }
            else
            {
                
            }
        }

        private void ExportData()
        {
            int saveWidth = chart2.Width, saveHeight = chart2.Height;
            int size;
            if (timeOfMeasurement < 150) size = 500;
            else if (timeOfMeasurement < 500) size = 600;
            else if (timeOfMeasurement < 1000) size = 700;
            else if (timeOfMeasurement < 1500) size = 800;
            else if (timeOfMeasurement < 2000) size = 900;
            else size = 1000;

            chart2.Width = chart3.Width = chart4.Width = chart5.Width = size;
            chart2.Height = chart3.Height = chart4.Height = chart5.Height = 500;

            numOfMeasurement++;

            string name0 = "\\" + textBox1.Text + " " + numOfMeasurement + " Data.txt";
            string name1 = "\\" + textBox1.Text + " " + numOfMeasurement + " Voltage.png";
            string name2 = "\\" + textBox1.Text + " " + numOfMeasurement + " Temperature.png";
            string name3 = "\\" + textBox1.Text + " " + numOfMeasurement + " Power.png";
            string name4 = "\\" + textBox1.Text + " " + numOfMeasurement + " Energy.png";
            string name5 = "\\" + textBox1.Text + " " + numOfMeasurement + " Output.txt";

            chart2.SaveImage(Settings.Default.SavePath.ToString() + name1, ChartImageFormat.Png);
            chart3.SaveImage(Settings.Default.SavePath.ToString() + name2, ChartImageFormat.Png);
            chart4.SaveImage(Settings.Default.SavePath.ToString() + name3, ChartImageFormat.Png);
            chart5.SaveImage(Settings.Default.SavePath.ToString() + name4, ChartImageFormat.Png);

            chart2.Width = chart3.Width = chart4.Width = chart5.Width = saveWidth; 
            chart2.Height = chart3.Height = chart4.Height = chart5.Height = saveHeight;

            string output = label1.Text + "\n" + label4.Text + "\n" + label5.Text + "\n" + label8.Text + "\n" + label2.Text + "\n" + label3.Text + "\n" + label6.Text + "\n" + label9.Text + "\n" + label10.Text + "\n" + label11.Text;
            File.WriteAllText(Settings.Default.SavePath.ToString() + name0, richTextBox1.Text);
            File.WriteAllText(Settings.Default.SavePath.ToString() + name5, output);

            label12.Text = (numOfMeasurement+1) + " Data.txt";
        }

        private void DeleteData()
        {
            chart2.Series[0].Points.Clear();
            chart3.Series[0].Points.Clear();
            chart3.Series[1].Points.Clear();
            chart3.Series[2].Points.Clear();
            chart3.Series[3].Points.Clear();
            chart4.Series[0].Points.Clear();
            chart5.Series[0].Points.Clear();
            richTextBox1.Clear();
            energy = 0;
            timeOfMeasurement= 1;
            averageTempDifference = 0;
            averageCold = 0;
            averageHot= 0;
        }

        private void buttonSettings_Click(object sender, EventArgs e) //open settings window
        {
            if (settingForm == null || settingForm.IsDisposed) //check if setting window does not exist
            {
                settingForm= new SettingForm();
                settingForm.Form1Instance= this;
                settingForm.Show();
            }
            else
            {
                settingForm.BringToFront();
            }

        }

        private void CheckStatusOfDevice()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                labelDeviceStatus.Text=("Device is online.");
                
            }
            else
            {
                labelDeviceStatus.Text = ("Device is offline.");
            }
        }

        public void SendCommand(object sender, EventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                Button clickedButton = sender as Button;
                string command = clickedButton.Name;
                command = command.Trim('B');
                Console.WriteLine("Sending command: " + command);
                serialPort.WriteLine(command);
            }
            else
            {
                MessageBox.Show("Device is not connected");
            }
        }

        public void SendSettingsCommand(string settingsCommand)
        {
            if (serialPort != null)
            {
                try
                {
                    serialPort.WriteLine(settingsCommand);
                    Console.WriteLine("Sended command" + settingsCommand);
                }
                catch
                {
                    MessageBox.Show("Failed to change setting");
                }
            }
            else
            {
                MessageBox.Show("Serial port is not connected");
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            
            TimerSendSettings++;
            progressBar1.Value = 25 * TimerSendSettings;
            switch (TimerSendSettings)
            {
                case 1: 
                    int MaxTemperature = Settings.Default.maxTemperature + 10000;
                    SendSettingsCommand(MaxTemperature.ToString());
                    B1000.Enabled= false;
                    button3.Enabled= false;
                    B13001.Enabled= false;
                    B13002.Enabled= false;
                    break;
                case 2:
                    int DesiredTemperature = Settings.Default.desiredTemperature + 11000;
                    SendSettingsCommand(DesiredTemperature.ToString());
                    break;
                case 3: 
                    SendSettingsCommand(Settings.Default.operatingMode.ToString());
                    break;
                case 4:
                    timer1.Enabled = false;
                    B1000.Enabled = true;
                    button3.Enabled = true;
                    B13001.Enabled = true;
                    B13002.Enabled = true;
                    TimerSendSettings = 0;
                    break;
            }
            
        }


        private void button2_Click(object sender, EventArgs e) //delete data
        {
            DialogResult result;
            result = MessageBox.Show("Delete all data?", "Delete data", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                DeleteData();
            }
            else if (result == DialogResult.No)
            {
                this.DialogResult = DialogResult.Cancel;
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            SendSettingsCommand("12000");
        }

        private void dataReceived(float receivedVoltage, float receivedTempHot, float receivedTempCold,int recievedTempDesired, int recievedTempMax)
        {
            this.chart2.Series["Voltage [mV]"].Points.AddY(receivedVoltage);
            chart3.Series["Hot side [°C]"].Points.AddY(receivedTempHot);
            chart3.Series["Cold side [°C]"].Points.AddY(receivedTempCold);
            chart3.Series["Desired [°C]"].Points.AddY(recievedTempDesired);
            chart3.Series["Maximal [°C]"].Points.AddY(recievedTempMax);
        }

        private void visualTemperatureChart(float actualTemperature)
        {
            this.chart1.Series["Maximal"].Points.Clear();
            this.chart1.Series["Desired"].Points.Clear();
            this.chart1.Series["Actual"].Points.Clear();
            if (Settings.Default.operatingMode == 1001)
            {
                this.chart1.Series["Maximal"].Points.AddXY(1, Settings.Default.maxTemperature);
                this.chart1.Series["Desired"].Points.AddXY(1, Settings.Default.desiredTemperature);
                this.chart1.Series["Actual"].Points.AddXY(1, actualTemperature);
            }
            else if (Settings.Default.operatingMode==1002)
            {
                this.chart1.Series["Maximal"].Points.AddXY(1, Settings.Default.maxTemperature);
                this.chart1.Series["Desired"].Points.AddXY(1, Settings.Default.desiredTemperature+coldSide);
                this.chart1.Series["Actual"].Points.AddXY(1, actualTemperature);
            }
            
        }

        private void Calculate(float milivolt)
        {
            float resistance;
            if (Settings.Default.Resistance != "")
            {
                resistance = float.Parse(Settings.Default.Resistance);
            }
            else resistance = 0;

            timeOfMeasurement++;

            if ((resistance > 0)&&(milivolt>0))
            {   
                
                float current = (milivolt / 1000) / resistance; //current in A
                float power = current * (milivolt / 1000);      //power in W
                energy += (power / 3600);                       //enerhy in  Wh
                label4.Text = "Current: " + current * 1000 + "mA";
                label5.Text = "Power: " + power * 1000 + "mW";
                chart4.Series["Power [mW]"].Points.AddY(power*1000);
                chart5.Series["Energy [mWh]"].Points.AddY(energy*1000);
                
            }
            else
            {
                label4.Text = "Current: 0mA";
                label5.Text = "Power: 0mW";
                chart4.Series["Power [mW]"].Points.AddY(0);
                chart5.Series["Energy [mWh]"].Points.AddY(energy*1000);

            }
            label8.Text = "Energy " + (energy * 1000) + "mWh";
            label9.Text = "Time: " + timeOfMeasurement + "s";
        }

    }
}
