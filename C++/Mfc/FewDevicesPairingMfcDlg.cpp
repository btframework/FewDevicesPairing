
// FewDevicesPairingMfcDlg.cpp : implementation file
//

#include "stdafx.h"
#include "FewDevicesPairingMfc.h"
#include "FewDevicesPairingMfcDlg.h"
#include "afxdialogex.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


CString IntToHex(const int i)
{
	CString s;
	s.Format(_T("%.8X"), i);
	return s;
}

CString IntToHex(const unsigned long i)
{
	CString s;
	s.Format(_T("%.8X"), i);
	return s;
}

CString IntToHex(const __int64 i)
{
	CString s;
	s.Format(_T("%.4X%.8X"), static_cast<INT32>((i >> 32) & 0x00000FFFF),
		static_cast<INT32>(i) & 0xFFFFFFFF);
	return s;
}

// CFewDevicesPairingMfcDlg dialog



CFewDevicesPairingMfcDlg::CFewDevicesPairingMfcDlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(IDD_FEWDEVICESPAIRINGMFC_DIALOG, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CFewDevicesPairingMfcDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_LIST_LOG, lbLog);
}

BEGIN_MESSAGE_MAP(CFewDevicesPairingMfcDlg, CDialogEx)
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_BN_CLICKED(IDC_BUTTON_START, &CFewDevicesPairingMfcDlg::OnBnClickedButtonStart)
	ON_BN_CLICKED(IDC_BUTTON_STOP, &CFewDevicesPairingMfcDlg::OnBnClickedButtonStop)
	ON_WM_DESTROY()
	ON_WM_TIMER()
END_MESSAGE_MAP()


// CFewDevicesPairingMfcDlg message handlers

BOOL CFewDevicesPairingMfcDlg::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon

	// TODO: Add extra initialization here
	FRadio = NULL;
	FTimerRunning = false;


	return TRUE;  // return TRUE  unless you set the focus to a control
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CFewDevicesPairingMfcDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialogEx::OnPaint();
	}
}

// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CFewDevicesPairingMfcDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}



void CFewDevicesPairingMfcDlg::OnBnClickedButtonStart()
{
	if (FRadio != NULL)
		lbLog.AddString(_T("Pairing running"));
	else
		Start();
}


void CFewDevicesPairingMfcDlg::OnBnClickedButtonStop()
{
	if (FRadio == NULL)
		lbLog.AddString(_T("Pairing not running"));
	else
		Stop();
}


void CFewDevicesPairingMfcDlg::OnDestroy()
{
	CDialogEx::OnDestroy();

	Stop();
}

void CFewDevicesPairingMfcDlg::ManagerAuthenticationCompleted(void* Sender,
	CwclBluetoothRadio* const Radio, const __int64 Address, const int Error)
{
	UNREFERENCED_PARAMETER(Sender);
	UNREFERENCED_PARAMETER(Radio);

	if (Address != FAddress)
	{
		lbLog.AddString(_T("Pairing with unknown device ") + IntToHex(Address) +
			_T(" completed with result: 0x") + IntToHex(Error));
	}
	else
	{
		if (Error == WCL_E_SUCCESS)
			lbLog.AddString(_T("Pairing with device completed with success"));
		else
			lbLog.AddString(_T("Pairing with device completed with error: 0x") + IntToHex(Error));

		if (FRadio != NULL)
		{
			lbLog.AddString(_T("Switching to next device"));
			StopTimer();
		}
	}
}

void CFewDevicesPairingMfcDlg::ManagerDeviceFound(void* Sender, CwclBluetoothRadio* const Radio,
	const __int64 Address)
{
	UNREFERENCED_PARAMETER(Sender);
	UNREFERENCED_PARAMETER(Radio);

	lbLog.AddString(_T("Device ") + IntToHex(Address) + _T(" found"));
	FDevices.push_back(Address);
}

void CFewDevicesPairingMfcDlg::ManagerDiscoveringCompleted(void* Sender,
	CwclBluetoothRadio* const Radio, const int Error)
{
	UNREFERENCED_PARAMETER(Sender);
	UNREFERENCED_PARAMETER(Radio);

	if (Error != WCL_E_SUCCESS)
	{
		lbLog.AddString(_T("Discovering completed with error: 0x") + IntToHex(Error));
		Stop();
	}
	else
	{
		lbLog.AddString(_T("Discovering comepleted"));
		if (FRadio != NULL)
		{
			if (FDevices.size() == 0)
			{
				lbLog.AddString(_T("No Bluetooth devices were found"));
				Stop();
			}
			else
			{
				lbLog.AddString(_T("Starting pairing"));
				PairWithNextDevice();
			}
		}
	}
}

void CFewDevicesPairingMfcDlg::ManagerDiscoveringStarted(void* Sender,
	CwclBluetoothRadio* const Radio)
{
	UNREFERENCED_PARAMETER(Sender);
	UNREFERENCED_PARAMETER(Radio);

	FDevices.clear();
	FCurrentDeviceIndex = 0;
	
	lbLog.AddString(_T("Discovering started"));
}

void CFewDevicesPairingMfcDlg::ManagerNumericComparison(void* Sender,
	CwclBluetoothRadio* const Radio, const __int64 Address, unsigned long Number,
	bool& Confirm)
{
	UNREFERENCED_PARAMETER(Sender);
	UNREFERENCED_PARAMETER(Radio);
	UNREFERENCED_PARAMETER(Number);

	Confirm = false;
	
	if (Address != FAddress)
	{
		lbLog.AddString(_T("Unknown device: ") + IntToHex(Address) +
			_T(" requires numeric comparison. Ignore"));
	}
	else
	{
		lbLog.AddString(_T("Device requires numeric comparison. Accept."));
		Confirm = true;
	}
}

void CFewDevicesPairingMfcDlg::ManagerPasskeyNotification(void* Sender,
	CwclBluetoothRadio* const Radio, const __int64 Address, unsigned long Passkey)
{
	UNREFERENCED_PARAMETER(Sender);
	UNREFERENCED_PARAMETER(Radio);
	UNREFERENCED_PARAMETER(Passkey);

	if (Address != FAddress)
	{
		lbLog.AddString(_T("Unknown device: ") + IntToHex(Address) +
			_T(" requires passkey entering. Ignore"));
	}
	else
		lbLog.AddString(_T("Device requires requires passkey entering."));
}

void CFewDevicesPairingMfcDlg::ManagerPasskeyRequest(void* Sender,
	CwclBluetoothRadio* const Radio, const __int64 Address, unsigned long& Passkey)
{
	UNREFERENCED_PARAMETER(Sender);
	UNREFERENCED_PARAMETER(Radio);

	Passkey = 0;
	if (Address != FAddress)
	{
		lbLog.AddString(_T("Unknown device: ") + IntToHex(Address) +
			_T(" requires passkey entering. Ignore"));
	}
	else
	{
		lbLog.AddString(_T("Device requires requires passkey providing."));
		Passkey = 1234; // here you must provide the passkey.
	}
}

void CFewDevicesPairingMfcDlg::ManagerPinRequest(void* Sender,
	CwclBluetoothRadio* const Radio, const __int64 Address, tstring& Pin)
{
	UNREFERENCED_PARAMETER(Sender);
	UNREFERENCED_PARAMETER(Radio);

	Pin = _T("");
	if (Address != FAddress)
	{
		lbLog.AddString(_T("Unknown device: ") + IntToHex(Address) +
			_T(" requires PIN. Ignore"));
	}
	else
	{
		lbLog.AddString(_T("Device requires requires PIN. Provdes: 0000"));
		Pin = _T("0000");
	}
}

void CFewDevicesPairingMfcDlg::PairWithNextDevice()
{
	StopTimer();

	while (true)
	{
		list<__int64>::iterator i = FDevices.begin();
		advance(i, FCurrentDeviceIndex);
		FAddress = *i;
		lbLog.AddString(_T("Try to pair with device: ") + IntToHex(FAddress));

		int Res = FRadio->RemotePair(FAddress);
		if (Res != WCL_E_SUCCESS)
			break;
		if (Res == WCL_E_BLUETOOTH_ALREADY_PAIRED)
			lbLog.AddString(_T("Device is already paired"));
		else
			lbLog.AddString(_T("Start pairing with device failed; 0x") + IntToHex(Res));

		FCurrentDeviceIndex++;
		if (FCurrentDeviceIndex == FDevices.size())
		{
			lbLog.AddString(_T("No more devices"));
			Stop();
			break;
		}
	}
}

void CFewDevicesPairingMfcDlg::Start()
{
	lbLog.ResetContent();

	int Res = FManager.Open();
	if (Res != WCL_E_SUCCESS)
		lbLog.AddString(_T("Unable to open Bluetooth Manager: 0x") + IntToHex(Res));
	else
	{
		if (FManager.Count == 0)
			lbLog.AddString(_T("Bluetooth hardware not found"));
		else
		{
			for (size_t i = 0; i < FManager.Count; i++)
			{
				if (FManager.Radios[i]->Available)
				{
					FRadio = FManager.Radios[i];
					break;
				}
			}

			if (FRadio == NULL)
				lbLog.AddString(_T("No available Bluetooth radio found"));
			else
			{
				lbLog.AddString(_T("Try to start discovering"));
				Res = FRadio->Discover(10, dkClassic);
				if (Res != WCL_E_SUCCESS)
				{
					lbLog.AddString(_T("Failed to start discovering: 0x") + IntToHex(Res));
					FRadio = NULL;
				}
			}
		}
		// If something went wrong we must close manager.
		if (FRadio == NULL)
			FManager.Close();
	}
}

void CFewDevicesPairingMfcDlg::Stop()
{
	// Cleanup Radio object.
	FRadio = NULL;
	// Stop timer.
	StopTimer();
	// Close Bluetooth Manager.
	FManager.Close();
	// Clear found devices list.
	FDevices.clear();
}

void CFewDevicesPairingMfcDlg::OnTimer(UINT_PTR nIDEvent)
{
	StopTimer();

	if (FRadio != NULL)
	{
		FCurrentDeviceIndex++;
		if (FCurrentDeviceIndex == FDevices.size())
		{
			lbLog.AddString(_T("No more devices"));
			Stop();
		}
		else
			PairWithNextDevice();
	}

	CDialogEx::OnTimer(nIDEvent);
}

void CFewDevicesPairingMfcDlg::StartTimer()
{
	if (!FTimerRunning)
	{
		SetTimer(100, 5000, NULL);
		FTimerRunning = true;
	}
}

void CFewDevicesPairingMfcDlg::StopTimer()
{
	if (FTimerRunning)
	{
		KillTimer(100);
		FTimerRunning = false;
	}
}