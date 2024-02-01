using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

using wclCommon;
using wclBluetooth;

namespace FewDevicesPairingWinForms
{
    public partial class fmMain : Form
    {
        private wclBluetoothManager FManager;
        private wclBluetoothRadio FRadio;
        private List<Int64> FDevices;
        private int FCurrentDeviceIndex;
        private Int64 FAddress;

        private void Stop()
        {
            // Cleanup Radio object.
            FRadio = null;
            // Stop timer.
            tiWait.Stop();
            // Close Bluetooth Manager.
            FManager.Close();
            // Clear found devices list.
            FDevices.Clear();
        }

        private void Start()
        {
            lbLog.Items.Clear();

            Int32 Res = FManager.Open();
            if (Res != wclErrors.WCL_E_SUCCESS)
                lbLog.Items.Add("Unable to open Bluetooth Manager: 0x" + Res.ToString("X8"));
            else
            {
                if (FManager.Count == 0)
                    lbLog.Items.Add("Bluetooth hardware not found");
                else
                {
                    for (int i = 0; i < FManager.Count; i++)
                    {
                        if (FManager[i].Available)
                        {
                            FRadio = FManager[i];
                            break;
                        }
                    }

                    if (FRadio == null)
                        lbLog.Items.Add("No available Bluetooth radio found");
                    else
                    {
                        lbLog.Items.Add("Try to start discovering");
                        Res = FRadio.Discover(10, wclBluetoothDiscoverKind.dkClassic);
                        if (Res != wclErrors.WCL_E_SUCCESS)
                        {
                            lbLog.Items.Add("Failed to start discovering: 0x" + Res.ToString("X8"));
                            FRadio = null;
                        }
                    }
                }

                // If something went wrong we must close manager.
                if (FRadio == null)
                    FManager.Close();
            }
        }

        private void PairWithNextDevice()
        {
            tiWait.Stop();

            while (true)
            {
                FAddress = FDevices[FCurrentDeviceIndex];
                lbLog.Items.Add("Try to pair with device: " + FAddress.ToString("X12"));

                Int32 Res = FRadio.RemotePair(FAddress);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    break;
                if (Res == wclBluetoothErrors.WCL_E_BLUETOOTH_ALREADY_PAIRED)
                    lbLog.Items.Add("Device is already paired");
                else
                    lbLog.Items.Add("Start pairing with device failed; 0x" + Res.ToString("X8"));

                FCurrentDeviceIndex++;
                if (FCurrentDeviceIndex == FDevices.Count)
                {
                    lbLog.Items.Add("No more devices");
                    Stop();
                    break;
                }
            }
        }

        public fmMain()
        {
            InitializeComponent();
        }

        private void fmMain_Load(object sender, EventArgs e)
        {
            FManager = new wclBluetoothManager();
            FManager.OnDiscoveringStarted += new wclBluetoothEvent(Manager_OnDiscoveringStarted);
            FManager.OnDeviceFound += new wclBluetoothDeviceEvent(Manager_OnDeviceFound);
            FManager.OnDiscoveringCompleted += new wclBluetoothResultEvent(Manager_OnDiscoveringCompleted);
            FManager.OnNumericComparison += new wclBluetoothNumericComparisonEvent(Manager_OnNumericComparison);
            FManager.OnPasskeyNotification += new wclBluetoothPasskeyNotificationEvent(Manager_OnPasskeyNotification);
            FManager.OnPasskeyRequest += new wclBluetoothPasskeyRequestEvent(Manager_OnPasskeyRequest);
            FManager.OnPinRequest += new wclBluetoothPinRequestEvent(Manager_OnPinRequest);
            FManager.OnAuthenticationCompleted += new wclBluetoothDeviceResultEvent(Manager_OnAuthenticationCompleted);

            FDevices = new List<Int64>();
            FRadio = null;
        }

        private void Manager_OnAuthenticationCompleted(object Sender, wclBluetoothRadio Radio, long Address, int Error)
        {
            if (Address != FAddress)
                lbLog.Items.Add("Pairing with unknown device " + Address.ToString("X12") + " completed with result: 0x" + Error.ToString("X8"));
            else
            {
                if (Error == wclErrors.WCL_E_SUCCESS)
                    lbLog.Items.Add("Pairing with device completed with success");
                else
                    lbLog.Items.Add("Pairing with device completed with error: 0x" + Error.ToString("X8"));

                if (FRadio != null)
                {
                    lbLog.Items.Add("Switching to next device");
                    tiWait.Start();
                }
            }
        }

        private void Manager_OnPinRequest(object Sender, wclBluetoothRadio Radio, long Address, out string Pin)
        {
            Pin = "";
            if (Address != FAddress)
                lbLog.Items.Add("Unknown device: " + Address.ToString("X12") + " requires PIN. Ignore");
            else
            {
                lbLog.Items.Add("Device requires requires PIN. Provdes: 0000");
                Pin = "0000";
            }
        }

        private void Manager_OnPasskeyRequest(object Sender, wclBluetoothRadio Radio, long Address, out uint Passkey)
        {
            Passkey = 0;
            if (Address != FAddress)
                lbLog.Items.Add("Unknown device: " + Address.ToString("X12") + " requires passkey entering. Ignore");
            else
            {
                lbLog.Items.Add("Device requires requires passkey providing.");
                Passkey = 1234; // here you must provide the passkey.
            }
        }

        private void Manager_OnPasskeyNotification(object Sender, wclBluetoothRadio Radio, long Address, uint Passkey)
        {
            if (Address != FAddress)
                lbLog.Items.Add("Unknown device: " + Address.ToString("X12") + " requires passkey entering. Ignore");
            else
                lbLog.Items.Add("Device requires requires passkey entering.");
        }

        private void Manager_OnNumericComparison(object Sender, wclBluetoothRadio Radio, long Address, uint Number, out bool Confirm)
        {
            Confirm = false;

            if (Address != FAddress)
                lbLog.Items.Add("Unknown device: " + Address.ToString("X12") + " requires numeric comparison. Ignore");
            else
            {
                lbLog.Items.Add("Device requires numeric comparison. Accept.");
                Confirm = true;
            }
        }

        private void Manager_OnDiscoveringCompleted(object Sender, wclBluetoothRadio Radio, int Error)
        {
            if (Error != wclErrors.WCL_E_SUCCESS)
            {
                lbLog.Items.Add("Discovering completed with error: 0x" + Error.ToString("X8"));
                Stop();
            }
            else
            {
                lbLog.Items.Add("Discovering comepleted");
                if (FRadio != null)
                {
                    if (FDevices.Count == 0)
                    {
                        lbLog.Items.Add("No Bluetooth devices were found");
                        Stop();
                    }
                    else
                    {
                        lbLog.Items.Add("Starting pairing");
                        PairWithNextDevice();
                    }
                }
            }
        }

        private void Manager_OnDeviceFound(object Sender, wclBluetoothRadio Radio, long Address)
        {
            lbLog.Items.Add("Device " + Address.ToString("X12") + " found");
            FDevices.Add(Address);
        }

        private void Manager_OnDiscoveringStarted(object Sender, wclBluetoothRadio Radio)
        {
            FDevices.Clear();
            FCurrentDeviceIndex = 0;

            lbLog.Items.Add("Discovering started");
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            if (FRadio == null)
                lbLog.Items.Add("Pairing not running");
            else
                Stop();
        }

        private void fmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Stop();
            FManager = null;
            FDevices = null;
        }

        private void btStart_Click(object sender, EventArgs e)
        {
            if (FRadio != null)
                lbLog.Items.Add("Pairing running");
            else
                Start();
        }

        private void tiWait_Tick(object sender, EventArgs e)
        {
            tiWait.Stop();

            if (FRadio != null)
            {
                FCurrentDeviceIndex++;
                if (FCurrentDeviceIndex == FDevices.Count)
                {
                    lbLog.Items.Add("No more devices");
                    Stop();
                }
                else
                    PairWithNextDevice();
            }
        }
    }
}