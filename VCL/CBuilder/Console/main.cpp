#include <vcl.h>
#include <windows.h>

#pragma hdrstop
#pragma argsused

#include <tchar.h>

#include <stdio.h>
#include <list>
#include <SyncObjs.hpp>
#include <iostream.h>
#include <conio.h>

#include "wclBluetooth.hpp"
#include "wclMessaging.hpp"
#include "wclBluetoothErrors.hpp"
#include "wclErrors.hpp"

using namespace std;

class TPairing
{
private:
	std::list<__int64>	FDevices;
	TEvent*				FOperationEvent;
	int					FOperationResult;
	__int64				FDeviceAddress;

	void __fastcall ManagerAuthenticationCompleted(TObject* Sender,
		TwclBluetoothRadio* const Radio, const __int64 Address,
		const int Error)
	{
		if (Address != FDeviceAddress)
		{
			wcout << "Pairing with unknown device " << hex << Address << dec <<
				" completed with result: 0x"  << hex << Error << dec << endl;
		}
		else
		{
			if (Error == WCL_E_SUCCESS)
				wcout << "Pairing with device completed with success" << endl;
			else
			{
				wcout << "Pairing with device completed with error: 0x" <<
					hex << Error << dec << endl;
			}

			// Switch to next device.
			FOperationEvent->SetEvent();
		}
	}

	void __fastcall ManagerPinRequest(TObject* Sender,
		TwclBluetoothRadio* const Radio, const __int64 Address, String& Pin)
	{
		Pin = "";
		if (Address != FDeviceAddress)
		{
			wcout << "Unknown device: " << hex << Address << dec <<
				" requires PIN. Ignore" << endl;
		}
		else
		{
			wcout << "Device requires requires PIN. Provdes: 0000" << endl;
			Pin = "0000";
		}
	}

	void __fastcall ManagerPasskeyRequest(TObject* Sender,
		TwclBluetoothRadio* const Radio, const __int64 Address,
		unsigned& Passkey)
	{
		Passkey = 0;
		if (Address != FDeviceAddress)
		{
			wcout << "Unknown device: " << hex <<Address << dec <<
				" requires passkey entering. Ignore" << endl;
		}
		else
		{
			wcout << "Device requires requires passkey providing." << endl;
			Passkey = 1234; // here you must provide the passkey.
		}
	}

	void __fastcall ManagerPasskeyNotification(TObject* Sender,
		TwclBluetoothRadio* const Radio, const __int64 Address,
		const unsigned Passkey)
	{
		if (Address != FDeviceAddress)
		{
			wcout << "Unknown device: " << hex << Address << dec <<
				" requires passkey entering. Ignore" << endl;
		}
		else
			wcout << "Device requires requires passkey entering." << endl;
	}

	void __fastcall ManagerNumericComparison(TObject* Sender,
		TwclBluetoothRadio* const Radio, const __int64 Address,
		const unsigned Number, bool& Confirm)
	{
		Confirm = false;

		if (Address != FDeviceAddress)
		{
			wcout << "Unknown device: " << hex << Address << dec <<
				" requires numeric comparison. Ignore" << endl;
		}
		else
		{
			wcout << "Device requires numeric comparison. Accept." << endl;
			Confirm = true;
		}
	}

	void __fastcall ManagerDiscoveringCompleted(TObject* Sender,
		TwclBluetoothRadio* const Radio, const int Error)
	{
		FOperationResult = Error;

		if (Error == WCL_E_SUCCESS)
			wcout << "Discovering compeleted with success" << endl;
		else
		{
			wcout << "Discovering completed with error: 0x" << hex << Error <<
				dec << endl;
		}
		FOperationEvent->SetEvent();
	}

	void __fastcall ManagerDeviceFound(TObject* Sender,
		TwclBluetoothRadio* const Radio, const __int64 Address)
	{
		wcout << "Device " << hex << Address << dec << " found" << endl;
		FDevices.push_back(Address);
	}

	void __fastcall ManagerDiscoveringStarted(TObject* Sender,
		TwclBluetoothRadio* const Radio)
	{
		wcout << "Discovering started" << endl;
	}

public:
	void __fastcall Start()
	{
		TwclMessageBroadcaster::SetSyncMethod(skThread);

		TwclBluetoothManager* Manager = new TwclBluetoothManager(NULL);
		Manager->OnDiscoveringStarted = ManagerDiscoveringStarted;
		Manager->OnDeviceFound = ManagerDeviceFound;
		Manager->OnDiscoveringCompleted = ManagerDiscoveringCompleted;
		Manager->OnNumericComparison = ManagerNumericComparison;
		Manager->OnPasskeyNotification = ManagerPasskeyNotification;
		Manager->OnPasskeyRequest = ManagerPasskeyRequest;
		Manager->OnPinRequest = ManagerPinRequest;
		Manager->OnAuthenticationCompleted = ManagerAuthenticationCompleted;

		int Res = Manager->Open();
		if (Res != WCL_E_SUCCESS)
		{
			wcout << "Unable to open Bluetooth Manager: 0x" << hex << Res <<
				dec << endl;
		}
		else
		{
			if (Manager->Count == 0)
				wcout << "Bluetooth hardware not found" << endl;
			else
			{
				TwclBluetoothRadio* Radio = NULL;

				for (int i = 0; i < Manager->Count; i++)
				{
					if (Manager->Radios[i]->Available)
					{
						Radio = Manager->Radios[i];
						break;
					}
				}

				if (Radio == NULL)
					wcout << "No Bluetooth radio available" << endl;
				else
				{
					wcout << "Found " << Radio->ApiName.c_str() <<
						" Bluetooth radio" << endl;

					FDevices.clear();
					FOperationEvent = new TEvent(NULL, false, false, "", false);
					FOperationResult = WCL_E_SUCCESS;

					wcout << "Start discovering" << endl;
					Res = Radio->Discover(10, dkClassic);
					if (Res != WCL_E_SUCCESS)
					{
						wcout << "Unable to start discovering: 0x" << hex <<
							Res << dec << endl;
					}
					else
					{
						wcout << "Wait for discovering completion" << endl;
						FOperationEvent->WaitFor(INFINITE);

						if (FOperationResult == WCL_E_SUCCESS)
						{
							if (FDevices.size() == 0)
								wcout << "No devices were found" << endl;
							else
							{
								wcout << "Starting pairing with all found devices" << endl;

								for (list<__int64>::iterator i = FDevices.begin(); i != FDevices.end(); i++)
								{
									FDeviceAddress = *i;
									wcout << "Start pairing with device: " << hex <<
										FDeviceAddress << dec << endl;
									Res = Radio->RemotePair(FDeviceAddress);
									if (Res == WCL_E_BLUETOOTH_ALREADY_PAIRED)
									{
										wcout << "Device already paired" << endl;
										continue;
									}

									if (Res != WCL_E_SUCCESS)
									{
										wcout << "Unable to pair with device: 0x" <<
											hex << Res << dec << endl;
										continue;
									}

									wcout << "Wait for pairing result" << endl;
									FOperationEvent->WaitFor(INFINITE);

									// We have to wait a little bit before switching to next device.
									Sleep(5000);
								}
								wcout << "Pairing completed" << endl;
							}
						}
					}
					FOperationEvent->Free();
				}
			}
			Manager->Close();
		}
		Manager->Free();
	}
};

int _tmain(int argc, _TCHAR* argv[])
{
	TPairing* Pairing = new TPairing();
	Pairing->Start();
	delete Pairing;

	return 0;
}
