﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SerialReaderSuperSimple
{
    public partial class frmMainWindow : Form
    {
        ConcurrentQueue<byte[]> outputDataQueue = new ConcurrentQueue<byte[]>();
        ConcurrentQueue<int> xAxisQueue = new ConcurrentQueue<int>();
        ConcurrentQueue<int> yAxisQueue = new ConcurrentQueue<int>();
        ConcurrentQueue<int> zAxisQueue = new ConcurrentQueue<int>();
        byte[] readBytes;
        byte[] removedItemInDataQueue;
        int removedItemInXAxisQueue;
        int removedItemInYAxisQueue;
        int removedItemInZAxisQueue;
        int MAX_AVERAGE_N = 100;
        int ORIENTATION_AVERAGE_N = 10;
        int GRAVITY_CALIBRATION_NUM = 30;
        int lastN;
        Stopwatch triggerStopwatch = new Stopwatch();
        Stopwatch sequenceStopwatch = new Stopwatch();

        ConcurrentQueue<int> bytesToReadQueue = new ConcurrentQueue<int>();
        int removedBytesToRead;
        int byteSequence = 0;

        int GESTURE_THRESHOLD = 220;
        
        SoundPlayer clefairyDancing = new SoundPlayer(@"C:\Users\davidwong\Desktop\Clefairy Pictures\Clefairy_Dancing_Sound.wav");
        SoundPlayer clefairySaysClassStarting = new SoundPlayer(@"C:\Users\davidwong\Desktop\Clefairy Pictures\Clefairy_Class_Starting.wav");

        public frmMainWindow()
        {
            InitializeComponent();
            readBytes = new byte[serCOM.ReadBufferSize];
            serCOM.BaudRate = 128000;
            serCOM.DataBits = 8;
            serCOM.StopBits = System.IO.Ports.StopBits.One;
            serCOM.Parity = System.IO.Ports.Parity.None;

            lastN = MAX_AVERAGE_N - ORIENTATION_AVERAGE_N;
            
        }

        private void cmbbxCOMPorts_DropDown(object sender, EventArgs e)
        {
            string[] COMPortNames = System.IO.Ports.SerialPort.GetPortNames().ToArray();
            cmbbxCOMPorts.Items.Clear();
            for (int cntr = 0; cntr < COMPortNames.Length; cntr++)
                cmbbxCOMPorts.Items.Add(COMPortNames[cntr]);
        }

        private void btnConnectDisconnect_Click(object sender, EventArgs e)
        {
            if(serCOM.IsOpen)
            {
                lock (serCOM) serCOM.Close();             // Mutex on the serial port
                btnConnectDisconnect.Text = "Connect";    // Changes button text to 'connect'
                lstbxSerParam.Items.Clear();              //
                cmbbxCOMPorts.Enabled = true;             // Enables clicking on the combo box for COM ports
                tmrSerRead.Enabled = false;               // Disables the timer for reading concurrent queue from serial port
				
                txtXAxis.Clear();                         // Clear txtXAxis box
                txtYAxis.Clear();                         // Clear txtYAxis box
                txtZAxis.Clear();                         // Clear txtZAxis box
            }
            else
            {
                if(!string.IsNullOrWhiteSpace(cmbbxCOMPorts.Text))
                {
                    serCOM.PortName = cmbbxCOMPorts.Text;
                    serCOM.Open();
                    btnConnectDisconnect.Text = "Disconnect";
                    lstbxSerParam.Items.Add("Baud Rate: " + serCOM.BaudRate.ToString());
                    lstbxSerParam.Items.Add("Data Bits: " + serCOM.DataBits.ToString());
                    lstbxSerParam.Items.Add("Stop bit: " + serCOM.StopBits.ToString());
                    lstbxSerParam.Items.Add("Parity: " + serCOM.Parity.ToString());
                    lstbxSerParam.Items.Add("Read Buffer Size: " + serCOM.ReadBufferSize.ToString());
                    lstbxSerParam.Items.Add("Received Bytes Threshold: " + serCOM.ReceivedBytesThreshold.ToString());
                    cmbbxCOMPorts.Enabled = false;
                    tmrSerRead.Enabled = true;
                }
                else
                {
                    MessageBox.Show("No COM port selected. Please try again.", "ERROR");
                }

            }
        }

        private void tmrSerRead_Tick(object sender, EventArgs e)
        {
            if (outputDataQueue.TryDequeue(out removedItemInDataQueue))
            {
                if (bytesToReadQueue.TryDequeue(out removedBytesToRead))
                    txtbxBytesToRead.Text = Convert.ToString(removedBytesToRead);
                txtXAxis.Text = Convert.ToString(removedItemInDataQueue[0]);
                txtYAxis.Text = Convert.ToString(removedItemInDataQueue[1]);
                txtZAxis.Text = Convert.ToString(removedItemInDataQueue[2]);

                int xAccel = Convert.ToInt16(txtXAxis.Text) - 128;
                int yAccel = Convert.ToInt16(txtYAxis.Text) - 128;
                int zAccel = Convert.ToInt16(txtZAxis.Text) - 128;

                if (FLAG_FIRST_VALUE == 0)
                {
                    FLAG_FIRST_VALUE = 1;
                    previousXValue = xAccel;
                    previousYValue = yAccel;
                    previousZValue = zAccel;
                }
                else
                {
                    if (!triggerStopwatch.IsRunning)
                    {
                        if (xAccel - previousXValue > GESTURE_THRESHOLD)
                        {
                            chkbxGestures.SetItemCheckState(0, CheckState.Checked);
                            triggerStopwatch.Start();
                            if (!sequenceStopwatch.IsRunning) sequenceStopwatch.Start();
                            gestureSequenceList.Add(0);
                        }
                        else if (xAccel - previousXValue < -GESTURE_THRESHOLD)
                        {
                            chkbxGestures.SetItemCheckState(3, CheckState.Checked);
                            triggerStopwatch.Start();
                            if (!sequenceStopwatch.IsRunning) sequenceStopwatch.Start();
                            gestureSequenceList.Add(3);
                        }
                        else if (yAccel - previousYValue > GESTURE_THRESHOLD)
                        {
                            chkbxGestures.SetItemCheckState(1, CheckState.Checked);
                            picClefairy.Image = imglstClefairy.Images[1];
                            clefairyDancing.Play();
                            triggerStopwatch.Start();
                            if (!sequenceStopwatch.IsRunning) sequenceStopwatch.Start();
                            gestureSequenceList.Add(1);
                        }
                        else if (yAccel - previousYValue < -GESTURE_THRESHOLD)
                        {
                            chkbxGestures.SetItemCheckState(4, CheckState.Checked);
                            picClefairy.Image = imglstClefairy.Images[2];
                            clefairyDancing.Play();
                            triggerStopwatch.Start();
                            if (!sequenceStopwatch.IsRunning) sequenceStopwatch.Start();
                            gestureSequenceList.Add(4);
                        }
                        else if (zAccel - previousZValue > GESTURE_THRESHOLD)
                        {
                            chkbxGestures.SetItemCheckState(5, CheckState.Checked);
                            picClefairy.Image = imglstClefairy.Images[0];
                            clefairyDancing.Play();
                            triggerStopwatch.Start();
                            if (!sequenceStopwatch.IsRunning) sequenceStopwatch.Start();
                            gestureSequenceList.Add(5);
                        }
                        else if (zAccel - previousZValue < -GESTURE_THRESHOLD)
                        {
                            chkbxGestures.SetItemCheckState(2, CheckState.Checked);
                            picClefairy.Image = imglstClefairy.Images[3];
                            clefairyDancing.Play();
                            triggerStopwatch.Start();
                            if (!sequenceStopwatch.IsRunning) sequenceStopwatch.Start();
                            gestureSequenceList.Add(2);
                        }
                    }

                    previousXValue = xAccel;
                    previousYValue = yAccel;
                    previousZValue = zAccel;

                    if (triggerStopwatch.ElapsedMilliseconds > 100)
                    {
                        triggerStopwatch.Reset();
                    }
                    if (sequenceStopwatch.ElapsedMilliseconds > 2000)
                    {
                        sequenceStopwatch.Reset();
                        chkbxGestures.SetItemCheckState(0, CheckState.Unchecked);
                        chkbxGestures.SetItemCheckState(1, CheckState.Unchecked);
                        chkbxGestures.SetItemCheckState(2, CheckState.Unchecked);
                        chkbxGestures.SetItemCheckState(3, CheckState.Unchecked);
                        chkbxGestures.SetItemCheckState(4, CheckState.Unchecked);
                        chkbxGestures.SetItemCheckState(5, CheckState.Unchecked);
                        if (gestureSequenceList.SequenceEqual(gestureSimplePunch)) txtbxGestureSequence.Text = "Simple punch";
                        else if (gestureSequenceList.SequenceEqual(gestureRightHook)) txtbxGestureSequence.Text = "Right hook";
                        else txtbxGestureSequence.Text = "***UNRECOGNIZED SEQUENCE***";
                        gestureSequenceList.Clear();
                    }
                }
                if (xAccel < -GRAVITY_CALIBRATION_NUM) txtbxOrientation.Text = "X";
                else if (xAccel > GRAVITY_CALIBRATION_NUM) txtbxOrientation.Text = "-X";
                else if (yAccel < -GRAVITY_CALIBRATION_NUM) txtbxOrientation.Text = "Y";
                else if (yAccel > GRAVITY_CALIBRATION_NUM) txtbxOrientation.Text = "-Y";
                else if (zAccel < -GRAVITY_CALIBRATION_NUM) txtbxOrientation.Text = "-Z";
                else if (zAccel > GRAVITY_CALIBRATION_NUM) txtbxOrientation.Text = "Z";
                else txtbxOrientation.Clear();

                xAxisQueue.Enqueue(Convert.ToInt16(txtXAxis.Text));
                yAxisQueue.Enqueue(Convert.ToInt16(txtYAxis.Text));
                zAxisQueue.Enqueue(Convert.ToInt16(txtZAxis.Text));

                if (xAxisQueue.Count() > MAX_AVERAGE_N)
                {
                    xAxisQueue.TryDequeue(out removedItemInXAxisQueue);
                    yAxisQueue.TryDequeue(out removedItemInYAxisQueue);
                    zAxisQueue.TryDequeue(out removedItemInZAxisQueue);


                    chrtAccelData.Series["XAxisData"].Points.RemoveAt(0);
                    chrtAccelData.Series["YAxisData"].Points.RemoveAt(0);
                    chrtAccelData.Series["ZAxisData"].Points.RemoveAt(0);
                }

                chrtAccelData.Series["XAxisData"].Points.AddY(Convert.ToInt16(removedItemInDataQueue[0]));
                chrtAccelData.Series["YAxisData"].Points.AddY(Convert.ToInt16(removedItemInDataQueue[1]));
                chrtAccelData.Series["ZAxisData"].Points.AddY(Convert.ToInt16(removedItemInDataQueue[2]));

                txtXAvg.Text = Convert.ToString(xAxisQueue.Average());
                txtYAvg.Text = Convert.ToString(yAxisQueue.Average());
                txtZAvg.Text = Convert.ToString(zAxisQueue.Average());
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            lock (serCOM) serCOM.Close();
        }

        private void serCOM_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            //int bytesToRead = serCOM.BytesToRead;
            //bytesToReadQueue.Enqueue(bytesToRead);
            //if (serCOM.ReadByte() == 255)
            //{
            //    serCOM.Read(readBytes, 0, readBytes.Length);
            //    outputDataQueue.Enqueue(readBytes);
            //}
            lock (serCOM)
            {
                int newByte;
                int bytesToRead = serCOM.BytesToRead;
                bytesToReadQueue.Enqueue(bytesToRead);
                while (bytesToRead != 0 && serCOM.IsOpen == true)
                {
                    newByte = serCOM.ReadByte();
                    if (newByte == 255) byteSequence = 0;
                    else
                    {
                        readBytes[byteSequence] = Convert.ToByte(newByte);
                        bytesToRead = serCOM.BytesToRead;
                        byteSequence++;
                    }
                }
                outputDataQueue.Enqueue(readBytes);
            }
        }

        private void btnClefairySays_Click(object sender, EventArgs e)
        {
            clefairySaysClassStarting.Play();
            pictureBox1.Enabled = true;
        }
    }
}
