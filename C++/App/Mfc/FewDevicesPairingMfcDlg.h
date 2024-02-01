
// FewDevicesPairingMfcDlg.h : header file
//

#pragma once
#include "afxwin.h"
#include <list>
#include "wclBluetooth.h"

using namespace std;
using namespace wclCommon;
using namespace wclBluetooth;


// CFewDevicesPairingMfcDlg dialog
class CFewDevicesPairingMfcDlg : public CDialogEx
{
// Construction
public:
	CFewDevicesPairingMfcDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_FEWDEVICESPAIRINGMFC_DIALOG };
#endif

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support


// Implementation
protected:
	HICON m_hIcon;

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()

private:
	CListBox lbLog;

private:
	CwclBluetoothManager FManager;
	CwclBluetoothRadio* FRadio;
	list<__int64> FDevices;
	int FCurrentDeviceIndex;
	__int64 FAddress;
	bool FTimerRunning;

	void ManagerAuthenticationCompleted(void* Sender, CwclBluetoothRadio* const Radio,
		const __int64 Address, const int Error);
	void ManagerPinRequest(void* Sender, CwclBluetoothRadio* const Radio,
		const __int64 Address, tstring& Pin);
	void ManagerPasskeyRequest(void* Sender, CwclBluetoothRadio* const Radio,
		const __int64 Address, unsigned long& Passkey);
	void ManagerPasskeyNotification(void* Sender, CwclBluetoothRadio* const Radio,
		const __int64 Address, unsigned long Passkey);
	void ManagerNumericComparison(void* Sender, CwclBluetoothRadio* const Radio,
		const __int64 Address, unsigned long Number, bool& Confirm);
	void ManagerDiscoveringCompleted(void* Sender, CwclBluetoothRadio* const Radio,
		const int Error);
	void ManagerDeviceFound(void* Sender, CwclBluetoothRadio* const Radio, const __int64 Address);
	void ManagerDiscoveringStarted(void* Sender, CwclBluetoothRadio* const Radio);

	void Stop();
	void Start();
	void PairWithNextDevice();

	void StartTimer();
	void StopTimer();

public:
	afx_msg void OnBnClickedButtonStart();
	afx_msg void OnBnClickedButtonStop();
	afx_msg void OnDestroy();
	afx_msg void OnTimer(UINT_PTR nIDEvent);
};
