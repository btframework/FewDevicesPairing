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
        private wclBluetoothManager Manager;
        private wclBluetoothRadio Radio;
        private List<Int64> Devices;
        private int CurrentDeviceIndex;
        private Int64 Address;

        private void Stop()
        {
            // Cleanup Radio object.
            Radio = null;
            // Stop timer.
            tiWait.Stop();
            // Close Bluetooth Manager.
            Manager.Close();
            // Clear found devices list.
            Devices.Clear();
        }

        private void Start()
        {
            lbLog.Items.Clear();

            Int32 Res = Manager.Open();
            if (Res != wclErrors.WCL_E_SUCCESS)
                lbLog.Items.Add("Unable to open Bluetooth Manager: 0x" + Res.ToString("X8"));
            else
            {
                if (Manager.Count == 0)
                    lbLog.Items.Add("Bluetooth hardware not found");
                else
                {
                    for (int i = 0; i < Manager.Count; i++)
                    {
                        if (Manager[i].Available)
                        {
                            Radio = Manager[i];
                            break;
                        }
                    }

                    if (Radio == null)
                        lbLog.Items.Add("No available Bluetooth radio found");
                    else
                    {
                        lbLog.Items.Add("Try to start discovering");
                        Res = Radio.Discover(10, wclBluetoothDiscoverKind.dkClassic);
                        if (Res != wclErrors.WCL_E_SUCCESS)
                        {
                            lbLog.Items.Add("Failed to start discovering: 0x" + Res.ToString("X8"));
                            Radio = null;
                        }
                    }
                }

                // If something went wrong we must close manager.
                if (Radio == null)
                    Manager.Close();
            }
        }

        private void PairWithNextDevice()
        {
            tiWait.Stop();

            while (true)
            {
                Address = Devices[CurrentDeviceIndex];
                lbLog.Items.Add("Try to pair with device: " + Address.ToString("X12"));

                Int32 Res = Radio.RemotePair(Address);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    break;
                if (Res == wclBluetoothErrors.WCL_E_BLUETOOTH_ALREADY_PAIRED)
                    lbLog.Items.Add("Device is already paired");
                else
                    lbLog.Items.Add("Start pairing with device failed; 0x" + Res.ToString("X8"));

                CurrentDeviceIndex++;
                if (CurrentDeviceIndex == Devices.Count)
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
            Manager = new wclBluetoothManager();
            Manager.OnDiscoveringStarted += new wclBluetoothEvent(Manager_OnDiscoveringStarted);
            Manager.OnDeviceFound += new wclBluetoothDeviceEvent(Manager_OnDeviceFound);
            Manager.OnDiscoveringCompleted += new wclBluetoothResultEvent(Manager_OnDiscoveringCompleted);
            Manager.OnNumericComparison += new wclBluetoothNumericComparisonEvent(Manager_OnNumericComparison);
            Manager.OnPasskeyNotification += new wclBluetoothPasskeyNotificationEvent(Manager_OnPasskeyNotification);
            Manager.OnPasskeyRequest += new wclBluetoothPasskeyRequestEvent(Manager_OnPasskeyRequest);
            Manager.OnPinRequest += new wclBluetoothPinRequestEvent(Manager_OnPinRequest);
            Manager.OnAuthenticationCompleted += new wclBluetoothDeviceResultEvent(Manager_OnAuthenticationCompleted);

            Devices = new List<Int64>();
            Radio = null;
        }

        private void Manager_OnAuthenticationCompleted(object Sender, wclBluetoothRadio Radio, long Address, int Error)
        {
            if (Address != this.Address)
                lbLog.Items.Add("Pairing with unknown device " + Address.ToString("X12") + " completed with result: 0x" + Error.ToString("X8"));
            else
            {
                if (Error == wclErrors.WCL_E_SUCCESS)
                    lbLog.Items.Add("Pairing with device completed with success");
                else
                    lbLog.Items.Add("Pairing with device completed with error: 0x" + Error.ToString("X8"));

                if (Radio != null)
                {
                    lbLog.Items.Add("Switching to next device");
                    tiWait.Start();
                }
            }
        }

        private void Manager_OnPinRequest(object Sender, wclBluetoothRadio Radio, long Address, out string Pin)
        {
            Pin = "";
            if (Address != this.Address)
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
            if (Address != this.Address)
                lbLog.Items.Add("Unknown device: " + Address.ToString("X12") + " requires passkey entering. Ignore");
            else
            {
                lbLog.Items.Add("Device requires requires passkey providing.");
                Passkey = 1234; // here you must provide the passkey.
            }
        }

        private void Manager_OnPasskeyNotification(object Sender, wclBluetoothRadio Radio, long Address, uint Passkey)
        {
            if (Address != this.Address)
                lbLog.Items.Add("Unknown device: " + Address.ToString("X12") + " requires passkey entering. Ignore");
            else
                lbLog.Items.Add("Device requires requires passkey entering.");
        }

        private void Manager_OnNumericComparison(object Sender, wclBluetoothRadio Radio, long Address, uint Number, out bool Confirm)
        {
            Confirm = false;

            if (Address != this.Address)
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
                if (Radio != null)
                {
                    if (Devices.Count == 0)
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
            Devices.Add(Address);
        }

        private void Manager_OnDiscoveringStarted(object Sender, wclBluetoothRadio Radio)
        {
            Devices.Clear();
            CurrentDeviceIndex = 0;

            lbLog.Items.Add("Discovering started");
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            if (Radio == null)
                lbLog.Items.Add("Pairing not running");
            else
                Stop();
        }

        private void fmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Stop();
            Manager = null;
            Devices = null;
        }

        private void btStart_Click(object sender, EventArgs e)
        {
            if (Radio != null)
                lbLog.Items.Add("Pairing running");
            else
                Start();
        }

        private void tiWait_Tick(object sender, EventArgs e)
        {
            tiWait.Stop();

            if (Radio != null)
            {
                CurrentDeviceIndex++;
                if (CurrentDeviceIndex == Devices.Count)
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