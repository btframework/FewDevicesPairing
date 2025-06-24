// FewDevicesPairingConsole.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

#include <stdio.h>
#include <list>
#include <iostream>

#include "wclBluetooth.h"

using namespace std;
using namespace wclCommon;
using namespace wclBluetooth;

class CPairing
{
private:
	list<__int64>	FDevices;
	HANDLE			FOperationEvent;
	int				FOperationResult;
	__int64			FDeviceAddress;
	
	void ManagerAuthenticationCompleted(void* Sender, CwclBluetoothRadio* const Radio,
		const __int64 Address, const int Error)
	{
		if (Address != FDeviceAddress)
		{
			cout << "Pairing with unknown device " << hex << Address << dec <<
				" completed with result: 0x" << hex << Error << dec << endl;
			return;
		}

		if (Error == WCL_E_SUCCESS)
			cout << "Pairing with device completed with success" << endl;
		else
		{
			cout << "Pairing with device completed with error: 0x" <<
				hex << Error << dec << endl;
		}

		// Switch to next device.
		SetEvent(FOperationEvent);
	}

	void ManagerPinRequest(void* Sender, CwclBluetoothRadio* const Radio,
		const __int64 Address, tstring& Pin)
	{
		Pin = _T("");
		if (Address != FDeviceAddress)
		{
			cout << "Unknown device: " << hex << Address << dec <<
				" requires PIN. Ignore" << endl;
			return;
		}
		
		cout << "Device requires requires PIN. Provdes: 0000" << endl;
		Pin = _T("0000");
	}

	void ManagerPasskeyRequest(void* Sender, CwclBluetoothRadio* const Radio, const __int64 Address,
		unsigned long& Passkey)
	{
		Passkey = 0;
		if (Address != FDeviceAddress)
		{
			cout << "Unknown device: " << hex << Address << dec <<
				" requires passkey entering. Ignore" << endl;
			return;
		}
		
		cout << "Device requires requires passkey providing." << endl;
		Passkey = 1234; // here you must provide the passkey.
	}

	void ManagerPasskeyNotification(void* Sender, CwclBluetoothRadio* const Radio,
		const __int64 Address, const unsigned long Passkey)
	{
		if (Address != FDeviceAddress)
		{
			cout << "Unknown device: " << hex << Address << dec <<
				" requires passkey entering. Ignore" << endl;
			return;
		}
		
		cout << "Device requires requires passkey entering." << endl;
	}

	void ManagerNumericComparison(void* Sender, CwclBluetoothRadio* const Radio,
		const __int64 Address, const unsigned long Number, bool& Confirm)
	{
		Confirm = false;
		
		if (Address != FDeviceAddress)
		{
			cout << "Unknown device: " << hex << Address << dec <<
				" requires numeric comparison. Ignore" << endl;
			return;
		}
		
		cout << "Device requires numeric comparison. Accept." << endl;
		Confirm = true;
	}

	void ManagerDiscoveringCompleted(void* Sender, CwclBluetoothRadio* const Radio, const int Error)
	{
		FOperationResult = Error;
		
		if (Error == WCL_E_SUCCESS)
			cout << "Discovering compeleted with success" << endl;
		else
		{
			cout << "Discovering completed with error: 0x" << hex << Error <<
				dec << endl;
		}

		SetEvent(FOperationEvent);
	}

	void ManagerDeviceFound(void* Sender, CwclBluetoothRadio* const Radio, const __int64 Address)
	{
		cout << "Device " << hex << Address << dec << " found" << endl;
		FDevices.push_back(Address);
	}

	void ManagerDiscoveringStarted(void* Sender, CwclBluetoothRadio* const Radio)
	{
		cout << "Discovering started" << endl;
	}

public:
	void Start()
	{
		CwclBluetoothManager* Manager = new CwclBluetoothManager();
		Manager->MessageProcessing = mpAsync;
		__hook(&CwclBluetoothManager::OnDiscoveringStarted, Manager, &CPairing::ManagerDiscoveringStarted);
		__hook(&CwclBluetoothManager::OnDeviceFound, Manager, &CPairing::ManagerDeviceFound);
		__hook(&CwclBluetoothManager::OnDiscoveringCompleted, Manager, &CPairing::ManagerDiscoveringCompleted);
		__hook(&CwclBluetoothManager::OnNumericComparison, Manager, &CPairing::ManagerNumericComparison);
		__hook(&CwclBluetoothManager::OnPasskeyNotification, Manager, &CPairing::ManagerPasskeyNotification);
		__hook(&CwclBluetoothManager::OnPasskeyRequest, Manager, &CPairing::ManagerPasskeyRequest);
		__hook(&CwclBluetoothManager::OnPinRequest, Manager, &CPairing::ManagerPinRequest);
		__hook(&CwclBluetoothManager::OnAuthenticationCompleted, Manager, &CPairing::ManagerAuthenticationCompleted);

		int Res = Manager->Open();
		if (Res != WCL_E_SUCCESS)
			cout << "Unable to open Bluetooth Manager: 0x" << hex << Res << dec << endl;
		else
		{
			if (Manager->Count == 0)
				cout << "Bluetooth hardware not found" << endl;
			else
			{
				CwclBluetoothRadio* Radio = NULL;

				for (size_t i = 0; i < Manager->Count; i++)
				{
					if (Manager->Radios[i]->Available)
					{
						Radio = Manager->Radios[i];
						break;
					}
				}

				if (Radio == NULL)
					cout << "No Bluetooth radio available" << endl;
				else
				{
					wcout << "Found " << Radio->ApiName.c_str() << " Bluetooth radio" << endl;

					FDevices.clear();
					FOperationEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
					FOperationResult = WCL_E_SUCCESS;

					cout << "Start discovering" << endl;
					Res = Radio->Discover(10, dkClassic);
					if (Res != WCL_E_SUCCESS)
					{
						cout << "Unable to start discovering: 0x" << hex << Res << dec << endl;
					}
					else
					{
						cout << "Wait for discovering completion" << endl;
						WaitForSingleObject(FOperationEvent, INFINITE);
						
						if (FOperationResult == WCL_E_SUCCESS)
						{
							if (FDevices.size() == 0)
								cout << "No devices were found" << endl;
							else
							{
								cout << "Starting pairing with all found devices" << endl;

								for (list<__int64>::iterator i = FDevices.begin(); i != FDevices.end(); i++)
								{
									FDeviceAddress = *i;
									cout << "Start pairing with device: " << hex << FDeviceAddress << dec << endl;
									Res = Radio->RemotePair(FDeviceAddress);
									if (Res == WCL_E_BLUETOOTH_ALREADY_PAIRED)
									{
										cout << "Device already paired" << endl;
										continue;
									}

									if (Res != WCL_E_SUCCESS)
									{
										cout << "Unable to pair with device: 0x" << hex << Res << dec << endl;
										continue;
									}

									cout << "Wait for pairing result" << endl;
									WaitForSingleObject(FOperationEvent, INFINITE);

									// We have to wait a little bit before switching to next device.
									Sleep(5000);
								}
								cout << "Pairing completed" << endl;
							}
						}
					}
					CloseHandle(FOperationEvent);
				}
			}
			Manager->Close();
		}
		__unhook(Manager);
		delete Manager;
	}
};


int main()
{
	CPairing* Pairing = new CPairing();
	Pairing->Start();
	delete Pairing;

    return 0;
}
