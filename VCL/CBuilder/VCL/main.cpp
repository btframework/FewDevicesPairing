//---------------------------------------------------------------------------

#include <vcl.h>
#pragma hdrstop

#include "main.h"
//---------------------------------------------------------------------------
#pragma package(smart_init)
#pragma link "wclBluetooth"
#pragma resource "*.dfm"
TfmMain *fmMain;
//---------------------------------------------------------------------------
__fastcall TfmMain::TfmMain(TComponent* Owner)
	: TForm(Owner)
{
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::btStartClick(TObject *Sender)
{
	if (FRadio != NULL)
		lbLog->Items->Add("Pairing running");
	else
		Start();
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::btStopClick(TObject *Sender)
{
	if (FRadio == NULL)
		lbLog->Items->Add("Pairing not running");
	else
		Stop();
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::FormCreate(TObject *Sender)
{
	FRadio = NULL;
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::FormDestroy(TObject *Sender)
{
	Stop();
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::ManagerAuthenticationCompleted(TObject *Sender,
	TwclBluetoothRadio * const Radio, const __int64 Address, const int Error)
{
	if (Address != FAddress)
	{
		lbLog->Items->Add("Pairing with unknown device " +
			IntToHex(Address, 12) + " completed with result: 0x" +
			IntToHex(Error, 8));
	}
	else
	{
		if (Error == WCL_E_SUCCESS)
			lbLog->Items->Add("Pairing with device completed with success");
		else
		{
			lbLog->Items->Add("Pairing with device completed with error: 0x" +
				IntToHex(Error, 8));
		}

		if (Radio != NULL)
		{
			lbLog->Items->Add("Switching to next device");
			tiWait->Enabled = true;
		}
	}
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::ManagerDeviceFound(TObject *Sender,
	TwclBluetoothRadio * const Radio, const __int64 Address)
{
	lbLog->Items->Add("Device " + IntToHex(Address, 12) + " found");
	FDevices.push_back(Address);
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::ManagerDiscoveringCompleted(TObject *Sender,
	TwclBluetoothRadio * const Radio, const int Error)
{
	if (Error != WCL_E_SUCCESS)
	{
		lbLog->Items->Add("Discovering completed with error: 0x" +
			IntToHex(Error, 8));
		Stop();
	}
	else
	{
		lbLog->Items->Add("Discovering comepleted");
		if (Radio != NULL)
		{
			if (FDevices.size() == 0)
			{
				lbLog->Items->Add("No Bluetooth devices were found");
				Stop();
			}
			else
			{
				lbLog->Items->Add("Starting pairing");
				PairWithNextDevice();
			}
		}
	}
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::ManagerDiscoveringStarted(TObject *Sender,
	TwclBluetoothRadio * const Radio)
{
	FDevices.clear();
	FCurrentDeviceIndex = 0;

	lbLog->Items->Add("Discovering started");
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::ManagerNumericComparison(TObject *Sender,
	TwclBluetoothRadio * const Radio, const __int64 Address, const DWORD Number,
	bool& Confirm)
{
	Confirm = false;

	if (Address != FAddress)
	{
		lbLog->Items->Add("Unknown device: " + IntToHex(Address, 12) +
			" requires numeric comparison. Ignore");
	}
	else
	{
		lbLog->Items->Add("Device requires numeric comparison. Accept.");
		Confirm = true;
	}
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::ManagerPasskeyNotification(TObject *Sender,
	TwclBluetoothRadio * const Radio, const __int64 Address,
	const DWORD Passkey)
{
	if (Address != FAddress)
	{
		lbLog->Items->Add("Unknown device: " + IntToHex(Address, 12) +
			" requires passkey entering. Ignore");
	}
	else
		lbLog->Items->Add("Device requires requires passkey entering.");
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::ManagerPasskeyRequest(TObject *Sender,
	TwclBluetoothRadio * const Radio, const __int64 Address, DWORD& Passkey)
{
	Passkey = 0;
	if (Address != FAddress)
	{
		lbLog->Items->Add("Unknown device: " + IntToHex(Address, 12) +
			" requires passkey entering. Ignore");
	}
	else
	{
		lbLog->Items->Add("Device requires requires passkey providing.");
		Passkey = 1234; // here you must provide the passkey.
	}
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::ManagerPinRequest(TObject *Sender,
	TwclBluetoothRadio * const Radio, const __int64 Address, UnicodeString& Pin)
{
	Pin = "";
	if (Address != FAddress)
	{
		lbLog->Items->Add("Unknown device: " + IntToHex(Address, 12) +
			" requires PIN. Ignore");
	}
	else
	{
		lbLog->Items->Add("Device requires requires PIN. Provdes: 0000");
		Pin = "0000";
	}
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::PairWithNextDevice()
{
	tiWait->Enabled = false;

	while (true)
	{
		std::list<__int64>::iterator i = FDevices.begin();
		std::advance(i, FCurrentDeviceIndex);
		FAddress = *i;
		lbLog->Items->Add("Try to pair with device: " + IntToHex(FAddress, 12));

		int Res = FRadio->RemotePair(FAddress);
		if (Res != WCL_E_SUCCESS)
			break;
		if (Res == WCL_E_BLUETOOTH_ALREADY_PAIRED)
			lbLog->Items->Add("Device is already paired");
		else
		{
			lbLog->Items->Add("Start pairing with device failed; 0x" +
				IntToHex(Res, 8));
		}

		FCurrentDeviceIndex++;
		if (FCurrentDeviceIndex == FDevices.size())
		{
			lbLog->Items->Add("No more devices");
			Stop();
			break;
		}
	}
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::Start()
{
	lbLog->Items->Clear();

	int Res = Manager->Open();
	if (Res != WCL_E_SUCCESS)
	{
		lbLog->Items->Add("Unable to open Bluetooth Manager: 0x" +
			IntToHex(Res, 8));
	}
	else
	{
		if (Manager->Count == 0)
			lbLog->Items->Add("Bluetooth hardware not found");
		else
		{
			for (int i = 0; i < Manager->Count; i++)
			{
				if (Manager->Radios[i]->Available)
				{
					FRadio = Manager->Radios[i];
					break;
				}
			}

			if (FRadio == NULL)
				lbLog->Items->Add("No available Bluetooth radio found");
			else
			{
				lbLog->Items->Add("Try to start discovering");
				Res = FRadio->Discover(10, dkClassic);
				if (Res != WCL_E_SUCCESS)
				{
					lbLog->Items->Add("Failed to start discovering: 0x" +
						IntToHex(Res, 8));
					FRadio = NULL;
				}
			}
		}
		// If something went wrong we must close manager.
		if (FRadio == NULL)
			Manager->Close();
	}
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::Stop()
{
	// Cleanup Radio object.
	FRadio = NULL;
	// Stop timer.
	tiWait->Enabled = false;
	// Close Bluetooth Manager.
	Manager->Close();
	// Clear found devices list.
	FDevices.clear();
}
//---------------------------------------------------------------------------
void __fastcall TfmMain::tiWaitTimer(TObject *Sender)
{
	  tiWait->Enabled = false;

	  if (FRadio != NULL)
	  {
		FCurrentDeviceIndex++;
		if (FCurrentDeviceIndex == FDevices.size())
		{
			lbLog->Items->Add("No more devices");
			Stop();
		}
		else
			PairWithNextDevice();
	  }
}
//---------------------------------------------------------------------------

