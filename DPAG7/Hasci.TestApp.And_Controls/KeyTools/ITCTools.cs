﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Microsoft.Win32;

using ITC_KEYBOARD;

namespace ITCTools
{
    public static class registrySettings
    {
        public enum CameraRes{
            low=0,
            med=1,
            high=2
        }
        public static CameraRes cameraResolution
        {
            get
            {
                CameraRes cRes = CameraRes.med;
                try
                {
                    int iRes = (int)Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\HasciTestApp", "SnapRes", 2);
                    cRes = (CameraRes)iRes;
                }
                catch (Exception)
                {
                    //could not read reg value                    
                }
                return cRes;
            }
        }
        public static CameraRes previewResolution
        {
            get
            {
                CameraRes cRes = CameraRes.med;
                try
                {
                    int iRes = (int)Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\HasciTestApp", "PreviewRes", 1);
                    cRes = (CameraRes)iRes;
                }
                catch (Exception)
                {
                    //could not read reg value                    
                }
                return cRes;
            }
        }
    }
    public static class KeyBoard
    {
        /// <summary>
        /// we have to save and restore the keyboard mapping of the scan button
        /// </summary>
        static ITC_KEYBOARD.CUSBkeys.usbKeyStruct _OldUsbKey = new ITC_KEYBOARD.CUSBkeys.usbKeyStruct();

        public static int createMultiKey()
        {
            addLog("########### createMultiKey #################");
            int iRet = -1;
            ITC_KEYBOARD.CUSBkeys _cusb = new ITC_KEYBOARD.CUSBkeys();
            //get current keyboard mapping reg location
            string sReg = ITC_KEYBOARD.CUSBkeys.getRegLocation();
            //count Multikey entries, remember: Multikey numbering starts by 1 (not zero)! 
            addLog("Opening subkey '" + sReg + "\\Multikeys'...");
            Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(sReg + "\\Multikeys", true);
            int iNumKeys = reg.ValueCount;
            addLog("Found " + iNumKeys.ToString() + " value entries");
            #region usbkeys
            //we need two entries in the last, new reg entry
            //one to fire a vkey: 
            //one to fire an named event
            ITC_KEYBOARD.CUSBkeys.usbKeyStruct _usbKeyVKEY = new CUSBkeys.usbKeyStruct();
            _usbKeyVKEY.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
            _usbKeyVKEY.bFlagMid = CUsbKeyTypes.usbFlagsMid.VKEY | CUsbKeyTypes.usbFlagsMid.NoRepeat;
            _usbKeyVKEY.bFlagLow = CUsbKeyTypes.usbFlagsLow.NormalKey;
            _usbKeyVKEY.bIntScan = (byte)ITC_KEYBOARD.VKEY.undef_0x88; //use vkey=0x88

            byte[] bytesVKey = { (byte)_usbKeyVKEY.bFlagHigh, (byte)_usbKeyVKEY.bFlagMid, (byte)_usbKeyVKEY.bFlagLow, (byte)_usbKeyVKEY.bIntScan };
            
            ITC_KEYBOARD.CUSBkeys.usbKeyStruct _usbKeyEVENT = new CUSBkeys.usbKeyStruct();
            _usbKeyEVENT.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
            _usbKeyEVENT.bFlagMid = CUsbKeyTypes.usbFlagsMid.NoRepeat | CUsbKeyTypes.usbFlagsMid.VKEY;
            _usbKeyEVENT.bFlagLow = CUsbKeyTypes.usbFlagsLow.NormalKey;
            _usbKeyEVENT.bIntScan = 0x88;
            byte[] bytesEventKey = { (byte)_usbKeyEVENT.bFlagHigh, (byte)_usbKeyEVENT.bFlagMid, (byte)_usbKeyEVENT.bFlagLow, (byte)_usbKeyEVENT.bIntScan };


            byte[] bytesAll = new byte[bytesVKey.Length + bytesEventKey.Length];
            System.Buffer.BlockCopy(bytesVKey, 0, bytesAll, 0, bytesVKey.Length);
            System.Buffer.BlockCopy(bytesEventKey, 0, bytesAll, bytesVKey.Length, bytesEventKey.Length);
            #endregion
            //check if the mutlikey is already in place
            string sValueName = "Multi" + iNumKeys.ToString("");
            byte[] bValues=null;
            try
            {
                bValues = (byte[])reg.GetValue(sValueName, null);
                addLog("GetValue last multikey ('"+ sValueName + "') OK");
            }
            catch (Exception ex)
            {
                addLog("GetValue last multikey ('"+ sValueName + "') failed" + ex.Message);
            }
            //write new mutlikey?
            if(bValues!=null){
                if (!compareBytes(bValues, bytesAll))
                {
                    //create a new entry
                    try
                    {
                        reg.SetValue("Multi" + (iNumKeys+1).ToString(), bytesAll, Microsoft.Win32.RegistryValueKind.Binary);
                        addLog("Creating new multikey value OK: " + reg.ToString() );
                    }
                    catch (Exception ex)
                    {
                        addLog("Creating new multikey value failed" + ex.Message);
                    }
                }
            }
            //change the scanner key to point to the new multikey index
            mapScanKey2Multi((byte)(iNumKeys+1));
            addLog("----------- createMultiKey -----------------");
            reg.Close();
            return iRet;
        }
        /// <summary>
        /// create a multikey event with two event pointers
        /// one points to StateLeftScan and DeltaLeftScan,
        /// other points to StateLeftScan1 and DeltaLeftScan1
        /// </summary>
        /// <returns></returns>
        public static int createMultiKey2Events()
        {
            addLog("########### createMultiKey #################");
            int iRet = -1;
            ITC_KEYBOARD.CUSBkeys _cusb = new ITC_KEYBOARD.CUSBkeys();
            //get current keyboard mapping reg location
            string sReg = ITC_KEYBOARD.CUSBkeys.getRegLocation();
            //create State1 and Delta1 evnet entries
            int newScanEventNumber = createScanEvents1();
            //count Multikey entries, remember: Multikey numbering starts by 1 (not zero)! 
            addLog("Opening subkey '" + sReg + "\\Multikeys'...");
            Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(sReg + "\\Multikeys", true);
            int iNumKeys = reg.ValueCount;
            addLog("Found " + iNumKeys.ToString() + " value entries");
            #region usbkeys
            //we need two entries in the last, new reg entry
            //one to fire a vkey: 
            //one to fire an named event
            ITC_KEYBOARD.CUSBkeys.usbKeyStruct _usbKeyVKEY = new CUSBkeys.usbKeyStruct();
            _usbKeyVKEY.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
            _usbKeyVKEY.bFlagMid = CUsbKeyTypes.usbFlagsMid.NoRepeat;
            _usbKeyVKEY.bFlagLow = CUsbKeyTypes.usbFlagsLow.NamedEventIndex;
            _usbKeyVKEY.bIntScan = 5;

            byte[] bytesVKey = { (byte)_usbKeyVKEY.bFlagHigh, (byte)_usbKeyVKEY.bFlagMid, (byte)_usbKeyVKEY.bFlagLow, (byte)_usbKeyVKEY.bIntScan };

            ITC_KEYBOARD.CUSBkeys.usbKeyStruct _usbKeyEVENT = new CUSBkeys.usbKeyStruct();
            _usbKeyEVENT.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
            _usbKeyEVENT.bFlagMid = CUsbKeyTypes.usbFlagsMid.NoRepeat;
            _usbKeyEVENT.bFlagLow = CUsbKeyTypes.usbFlagsLow.NamedEventIndex;
            _usbKeyEVENT.bIntScan = 1;
            byte[] bytesEventKey = { (byte)_usbKeyEVENT.bFlagHigh, (byte)_usbKeyEVENT.bFlagMid, (byte)_usbKeyEVENT.bFlagLow, (byte)_usbKeyEVENT.bIntScan };


            byte[] bytesAll = new byte[bytesVKey.Length + bytesEventKey.Length];
            System.Buffer.BlockCopy(bytesVKey, 0, bytesAll, 0, bytesVKey.Length);
            System.Buffer.BlockCopy(bytesEventKey, 0, bytesAll, bytesVKey.Length, bytesEventKey.Length);
            #endregion
            //check if the mutlikey is already in place
            string sValueName = "Multi" + iNumKeys.ToString("");
            byte[] bValues = null;
            try
            {
                bValues = (byte[])reg.GetValue(sValueName, null);
                addLog("GetValue last multikey ('" + sValueName + "') OK");
            }
            catch (Exception ex)
            {
                addLog("GetValue last multikey ('" + sValueName + "') failed" + ex.Message);
            }
            //write new mutlikey?
            if (bValues != null)
            {
                if (!compareBytes(bValues, bytesAll))
                {
                    //create a new entry
                    try
                    {
                        reg.SetValue("Multi" + (iNumKeys + 1).ToString(), bytesAll, Microsoft.Win32.RegistryValueKind.Binary);
                        addLog("Creating new multikey value OK: " + reg.ToString());
                        iNumKeys++;
                    }
                    catch (Exception ex)
                    {
                        addLog("Creating new multikey value failed" + ex.Message);
                    }
                }
            }
            //change the scanner key to point to the multikey index
            mapScanKey2Multi((byte)(iNumKeys));
            addLog("----------- createMultiKey -----------------");
            reg.Close();
            return iRet;
        }
        static int createScanEvents1()
        {
            int iRet = 0;
            //add two new events
            string sReg = ITC_KEYBOARD.CUSBkeys.getRegLocation();
            Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(sReg + "\\Events\\State", true);
            int iNumKeys = reg.ValueCount;
            //check if there is already a StateLeftScan1 entry
            bool bFound = false;
            string[] sValueNames = reg.GetValueNames();
            for (int i=1; i <= iNumKeys; i++ )
            {
                string t = (string)reg.GetValue(sValueNames[i-1]);
                if (t.Equals("StateLeftScan1", StringComparison.OrdinalIgnoreCase))
                {
                    bFound = true;
                    iRet = Convert.ToInt16(sValueNames[i-1].Substring("Event".Length));
                    break;
                }
            }
            if (!bFound) //create new entries
            {
                //for(int i=1; i<=sValueNames.Length;
                reg.SetValue("Event"+(iNumKeys+1).ToString(), "StateLeftScan1");
                reg = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(sReg + "\\Events\\Delta", true);
                reg.SetValue("Event" + (iNumKeys + 1).ToString(), "DeltaLeftScan1");
                iRet = iNumKeys + 1;
            }
            reg.Close();
            return iRet;
        }
        private static bool compareBytes(byte[] ba1, byte[] ba2)
        {
            if (ba1.Length != ba2.Length)
                return false;
            for (int i = 0; i < ba1.Length; i++)
            {
                if (ba1[i] != ba2[i])
                    return false;
            }
            return true;
        }
        /// <summary>
        /// change the event names of scanbutton to StateLeftScan1 and DeltaLeftScan1
        /// will also map all side buttons to scan
        /// </summary>
        public static void mapKey()
        {
            ITC_KEYBOARD.CUSBkeys _cusb = new ITC_KEYBOARD.CUSBkeys();
            ITC_KEYBOARD.CUSBkeys.usbKeyStruct _usbKey = new CUSBkeys.usbKeyStruct();
            //although we read the scan button setting here, we 'adjust' need to adjust it
            int iIdx = _cusb.getKeyStruct(0, CUsbKeyTypes.HWkeys.SCAN_Button_KeyLang1, ref _usbKey);

            //add two new events
            string sReg = ITC_KEYBOARD.CUSBkeys.getRegLocation();
            Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(sReg + "\\Events\\State", true);
            reg.SetValue("Event5", "StateLeftScan1");
            reg = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(sReg + "\\Events\\Delta", true);
            reg.SetValue("Event5", "DeltaLeftScan1");

            //change the scan button to fire these events
            if (iIdx != -1)
            {
                _OldUsbKey = _usbKey; //save for later restore
                //adjust the saved scan button:
                _OldUsbKey.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
                _OldUsbKey.bFlagMid = CUsbKeyTypes.usbFlagsMid.NoRepeat | CUsbKeyTypes.usbFlagsMid.Silent;
                _OldUsbKey.bFlagLow = CUsbKeyTypes.usbFlagsLow.NamedEventIndex;
                _OldUsbKey.bIntScan = 1;

                addLog("scanbutton key index is " + iIdx.ToString());

                //make a standard scan button
                _usbKey.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
                _usbKey.bFlagMid = CUsbKeyTypes.usbFlagsMid.NoRepeat | CUsbKeyTypes.usbFlagsMid.Silent;
                _usbKey.bFlagLow = CUsbKeyTypes.usbFlagsLow.NamedEventIndex;
                _usbKey.bIntScan = 5;   //let it point to our named Events

                for (int i = 0; i < _cusb.getNumPlanes(); i++)
                {
                    addLog("using plane: " + i.ToString());
                    if (_cusb.setKey(0, _usbKey.bScanKey, _usbKey) == 0)
                        addLog("setKey for scanbutton key OK");
                    else
                        addLog("setKey for scanbutton key failed");
                }
                _cusb.writeKeyTables();
                _cusb = null;
                mapAllSide2SCAN();
            }
            else
            {
                addLog("Could not get index for scanbutton key");
            }
        }


        /// <summary>
        /// restore scan button mapping to point to named event 1
        /// </summary>
        public static void restoreKey()
        {
            ITC_KEYBOARD.CUSBkeys _cusb = new ITC_KEYBOARD.CUSBkeys();
            ITC_KEYBOARD.CUSBkeys.usbKeyStruct _usbKey = new CUSBkeys.usbKeyStruct();
            int iIdx = _cusb.getKeyStruct(0, CUsbKeyTypes.HWkeys.SCAN_Button_KeyLang1, ref _usbKey);
            //change the scan button back to the original events
            if (iIdx != -1)
            {
                _usbKey = _OldUsbKey; //use saved var for restore
                addLog("scanbutton key index is " + iIdx.ToString());
                //_usbKey.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
                //_usbKey.bFlagMid = CUsbKeyTypes.usbFlagsMid.NOOP;
                //_usbKey.bFlagLow = CUsbKeyTypes.usbFlagsLow.NormalKey;
                _usbKey.bIntScan = 1;
                for (int iPlane = 0; iPlane < _cusb.getNumPlanes(); iPlane++)
                {
                    addLog("using plane: " + iPlane.ToString());
                    if (_cusb.setKey(iPlane, _usbKey.bScanKey, _usbKey) == 0) //changed "setKey(0," to "setKey(i,"
                        addLog("setKey for scanbutton key OK");
                    else
                        addLog("setKey for scanbutton key failed");
                }
                _cusb.writeKeyTables();
                mapAllSide2NOOP();                
            }
            else
            {
                addLog("Could not get index for scanbutton key");
            }
        }
        /*
            07,3F,00,02,02,01 'F6'  'F9' 'EventIndex'|'StateLeftScan'|'DeltaLeftScan
            07,40,00,02,02,01 'F7'  'F9' 'EventIndex'|'StateLeftScan'|'DeltaLeftScan
            07,43,00,02,02,01 'F10'  'F9' 'EventIndex'|'StateLeftScan'|'DeltaLeftScan
            07,91,00,02,02,01 'Keyboard Lang 2'  'F9' 'EventIndex'|'StateLeftScan'|'DeltaLeftScan
            07,90,00,02,02,01 'Keyboard Lang 1 (<SCAN>)'  'F9' 'EventIndex'|'StateLeftScan'|'DeltaLeftScan
        */
        /// <summary>
        /// read the scanbutton mapping and apply it to the side buttons
        /// </summary>
        private static void mapAllSide2SCAN()
        {
            //init the class
            ITC_KEYBOARD.CUSBkeys _cusbKeys = new ITC_KEYBOARD.CUSBkeys();

            //struct to hold key definition
            CUSBkeys.usbKeyStruct usbKey = new CUSBkeys.usbKeyStruct();

            //NORMAL Plane = 0x00
            //orange plane = 0x01
            //green/aqua plane = 0x02
            int iCount = _cusbKeys.getNumPlanes();

            //struct to hold key definition
            CUSBkeys.usbKeyStruct usbScanKey = new CUSBkeys.usbKeyStruct();
            //get main scan button 
            //int iIndex = _cusbKeys.getKeyIndex(0, (int)ITC_KEYBOARD.CUsbKeyTypes.HWkeys.SCAN_Button_KeyLang1 /*0x90*/);
            _cusbKeys.getKeyStruct(0, CUsbKeyTypes.HWkeys.SCAN_Button_KeyLang1, ref usbScanKey);

            //make a normal scan button
            usbScanKey.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
            usbScanKey.bFlagMid = CUsbKeyTypes.usbFlagsMid.NoRepeat | CUsbKeyTypes.usbFlagsMid.Silent;
            usbScanKey.bFlagLow = CUsbKeyTypes.usbFlagsLow.NamedEventIndex;
            usbScanKey.bIntScan = 5; //map to Event 5

            for (int iPlane = 0; iPlane < iCount; iPlane++) //do for all planes
            {
                //remap F6 to SCAN
                _cusbKeys.getKeyStruct(iPlane, ITC_KEYBOARD.CUsbKeyTypes.HWkeys.F6_VOL_UP, ref usbKey);
                usbKey.bFlagHigh = usbScanKey.bFlagHigh;
                usbKey.bFlagMid = usbScanKey.bFlagMid;
                usbKey.bFlagLow = usbScanKey.bFlagLow;
                usbKey.bIntScan = usbScanKey.bIntScan;
                _cusbKeys.setKey(iPlane, CUsbKeyTypes.HWkeys.F6_VOL_UP, usbKey);

                // F7
                _cusbKeys.getKeyStruct(iPlane, ITC_KEYBOARD.CUsbKeyTypes.HWkeys.F7_VOL_DN, ref usbKey);
                usbKey.bFlagHigh = usbScanKey.bFlagHigh;
                usbKey.bFlagMid = usbScanKey.bFlagMid;
                usbKey.bFlagLow = usbScanKey.bFlagLow;
                usbKey.bIntScan = usbScanKey.bIntScan; 
                _cusbKeys.setKey(iPlane, CUsbKeyTypes.HWkeys.F7_VOL_DN, usbKey);

                //Side Scan button: dec145, 0x91
                _cusbKeys.getKeyStruct(iPlane, 0x91, ref usbKey);
                usbKey.bFlagHigh = usbScanKey.bFlagHigh;
                usbKey.bFlagMid = usbScanKey.bFlagMid;
                usbKey.bFlagLow = usbScanKey.bFlagLow;
                usbKey.bIntScan = usbScanKey.bIntScan; 
                _cusbKeys.setKey(iPlane, 0x91, usbKey);

                //APP key: dec67, 0x43
                _cusbKeys.getKeyStruct(iPlane, 0x43, ref usbKey);
                usbKey.bFlagHigh = usbScanKey.bFlagHigh;
                usbKey.bFlagMid = usbScanKey.bFlagMid;
                usbKey.bFlagLow = usbScanKey.bFlagLow;
                usbKey.bIntScan = usbScanKey.bIntScan; 
                _cusbKeys.setKey(iPlane, 0x43, usbKey);

            }
            _cusbKeys.writeKeyTables();
        }
        /// <summary>
        /// read the scanbutton mapping and apply "Event Name 1" to all side buttons
        /// </summary>
        public static void mapAllSide2SCAN_Event1()
        {
            //init the class
            ITC_KEYBOARD.CUSBkeys _cusbKeys = new ITC_KEYBOARD.CUSBkeys();

            //struct to hold key definition
            CUSBkeys.usbKeyStruct usbKey = new CUSBkeys.usbKeyStruct();

            //NORMAL Plane = 0x00
            //orange plane = 0x01
            //green/aqua plane = 0x02
            int iCount = _cusbKeys.getNumPlanes();

            //struct to hold key definition
            CUSBkeys.usbKeyStruct usbScanKey = new CUSBkeys.usbKeyStruct();
            //get main scan button 
            //int iIndex = _cusbKeys.getKeyIndex(0, (int)ITC_KEYBOARD.CUsbKeyTypes.HWkeys.SCAN_Button_KeyLang1 /*0x90*/);
            _cusbKeys.getKeyStruct(0, CUsbKeyTypes.HWkeys.SCAN_Button_KeyLang1, ref usbScanKey);

            //make a normal scan button
            usbScanKey.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
            usbScanKey.bFlagMid = CUsbKeyTypes.usbFlagsMid.NoRepeat | CUsbKeyTypes.usbFlagsMid.Silent;
            usbScanKey.bFlagLow = CUsbKeyTypes.usbFlagsLow.NamedEventIndex;
            usbScanKey.bIntScan = 1; //map to Delta/State-LeftScan Event 1

            for (int iPlane = 0; iPlane < iCount; iPlane++) //do for all planes
            {
                //remap F6 to SCAN
                _cusbKeys.getKeyStruct(iPlane, ITC_KEYBOARD.CUsbKeyTypes.HWkeys.F6_VOL_UP, ref usbKey);
                usbKey.bFlagHigh = usbScanKey.bFlagHigh;
                usbKey.bFlagMid = usbScanKey.bFlagMid;
                usbKey.bFlagLow = usbScanKey.bFlagLow;
                usbKey.bIntScan = usbScanKey.bIntScan;
                _cusbKeys.setKey(iPlane, CUsbKeyTypes.HWkeys.F6_VOL_UP, usbKey);

                // F7
                _cusbKeys.getKeyStruct(iPlane, ITC_KEYBOARD.CUsbKeyTypes.HWkeys.F7_VOL_DN, ref usbKey);
                usbKey.bFlagHigh = usbScanKey.bFlagHigh;
                usbKey.bFlagMid = usbScanKey.bFlagMid;
                usbKey.bFlagLow = usbScanKey.bFlagLow;
                usbKey.bIntScan = usbScanKey.bIntScan;
                _cusbKeys.setKey(iPlane, CUsbKeyTypes.HWkeys.F7_VOL_DN, usbKey);

                //Side Scan button: dec145, 0x91
                _cusbKeys.getKeyStruct(iPlane, 0x91, ref usbKey);
                usbKey.bFlagHigh = usbScanKey.bFlagHigh;
                usbKey.bFlagMid = usbScanKey.bFlagMid;
                usbKey.bFlagLow = usbScanKey.bFlagLow;
                usbKey.bIntScan = usbScanKey.bIntScan;
                _cusbKeys.setKey(iPlane, 0x91, usbKey);

                //APP key: dec67, 0x43
                _cusbKeys.getKeyStruct(iPlane, 0x43, ref usbKey);
                usbKey.bFlagHigh = usbScanKey.bFlagHigh;
                usbKey.bFlagMid = usbScanKey.bFlagMid;
                usbKey.bFlagLow = usbScanKey.bFlagLow;
                usbKey.bIntScan = usbScanKey.bIntScan;
                _cusbKeys.setKey(iPlane, 0x43, usbKey);

            }
            _cusbKeys.writeKeyTables();
        }
        
        private static void mapAllSide2NOOP()
        {
            //init the class
            ITC_KEYBOARD.CUSBkeys _cusbKeys = new ITC_KEYBOARD.CUSBkeys();

            //struct to hold key definition
            CUSBkeys.usbKeyStruct usbKey = new CUSBkeys.usbKeyStruct();

            //NORMAL Plane = 0x00
            //orange plane = 0x01
            //green/aqua plane = 0x02
            int iCount = _cusbKeys.getNumPlanes();
            for (int iPlane = 0; iPlane < iCount; iPlane++) //do for all planes
            {
                //remap F6 to NOOP
                //new use: _cusbKeys.getKeyStruct(iPlane, HardwareKeys.CK70Keys.ITC_Standard_UpperRight_Btn, ref usbKey);
                _cusbKeys.getKeyStruct(iPlane, ITC_KEYBOARD.CUsbKeyTypes.HWkeys.F6_VOL_UP, ref usbKey);
                usbKey.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
                usbKey.bFlagMid = CUsbKeyTypes.usbFlagsMid.NOOP;
                usbKey.bFlagLow = CUsbKeyTypes.usbFlagsLow.NormalKey;
                _cusbKeys.setKey(iPlane, CUsbKeyTypes.HWkeys.F6_VOL_UP, usbKey);

                // F7
                _cusbKeys.getKeyStruct(iPlane, ITC_KEYBOARD.CUsbKeyTypes.HWkeys.F7_VOL_DN, ref usbKey);
                usbKey.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
                usbKey.bFlagMid = CUsbKeyTypes.usbFlagsMid.NOOP;
                usbKey.bFlagLow = CUsbKeyTypes.usbFlagsLow.NormalKey;
                _cusbKeys.setKey(iPlane, CUsbKeyTypes.HWkeys.F7_VOL_DN, usbKey);

                //Side Scan button: dec145, 0x91
                _cusbKeys.getKeyStruct(iPlane, 0x91, ref usbKey);
                usbKey.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
                usbKey.bFlagMid = CUsbKeyTypes.usbFlagsMid.NOOP;
                usbKey.bFlagLow = CUsbKeyTypes.usbFlagsLow.NormalKey;
                _cusbKeys.setKey(iPlane, 0x91, usbKey);

                //APP key: dec67, 0x43
                _cusbKeys.getKeyStruct(iPlane, 0x43, ref usbKey);
                usbKey.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
                usbKey.bFlagMid = CUsbKeyTypes.usbFlagsMid.NOOP;
                usbKey.bFlagLow = CUsbKeyTypes.usbFlagsLow.NormalKey;
                _cusbKeys.setKey(iPlane, 0x43, usbKey);

            }
            _cusbKeys.writeKeyTables();
        }
        /// <summary>
        /// for debug use we can log messages to DebugOut
        /// </summary>
        /// <param name="s"></param>
        static void addLog(string s)
        {
            System.Diagnostics.Debug.WriteLine(s);
        }
        /// <summary>
        /// just to debug, a function to dump a key struct
        /// </summary>
        /// <param name="cusb"></param>
        static void dumpKey(CUSBkeys.usbKeyStruct cusb)
        {
            addLog(string.Format(
                "Key struct is \n\tscankey={0}\n\tIntScan={1}\n\tflagHigh={2}\n\tflagMid={3}\n\tflagLow={4}",
                cusb.bScanKey.ToString(),
                cusb.bIntScan.ToString(),
                cusb.bFlagHigh.ToString(),
                cusb.bFlagMid.ToString(),
                cusb.bFlagLow.ToString()
                )
            );
        }
        /// <summary>
        /// restore scan button mapping to point to named event 1
        /// </summary>
        public static void restoreScanKeyDefault()
        {
            ITC_KEYBOARD.CUSBkeys _cusb = new ITC_KEYBOARD.CUSBkeys();
            ITC_KEYBOARD.CUSBkeys.usbKeyStruct _usbKey = new CUSBkeys.usbKeyStruct();
            int iIdx = _cusb.getKeyStruct(0, CUsbKeyTypes.HWkeys.SCAN_Button_KeyLang1, ref _usbKey);
            //change the scan button back to the original events
            if (iIdx != -1)
            {

                _usbKey.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
                _usbKey.bFlagMid = CUsbKeyTypes.usbFlagsMid.NoRepeat;// | CUsbKeyTypes.usbFlagsMid.Silent;
                _usbKey.bFlagLow = CUsbKeyTypes.usbFlagsLow.NamedEventIndex;
                _usbKey.bIntScan = 1;

                addLog("scanbutton key index is " + iIdx.ToString());
                //_usbKey.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
                //_usbKey.bFlagMid = CUsbKeyTypes.usbFlagsMid.NOOP;
                //_usbKey.bFlagLow = CUsbKeyTypes.usbFlagsLow.NormalKey;
                _usbKey.bIntScan = 1;
                for (int iPlane = 0; iPlane < _cusb.getNumPlanes(); iPlane++)
                {
                    addLog("using plane: " + iPlane.ToString());
                    if (_cusb.setKey(iPlane, _usbKey.bScanKey, _usbKey) == 0) //changed "setKey(0," to "setKey(i,"
                        addLog("setKey for scanbutton key OK");
                    else
                        addLog("setKey for scanbutton key failed");
                }
                _cusb.writeKeyTables();
                mapAllSide2NOOP();
            }
            else
            {
                addLog("Could not get index for scanbutton key");
            }
        }
        /// <summary>
        /// restore scan button mapping to point to named event 1
        /// </summary>
        public static void mapScanKey2Multi(byte b)
        {
            addLog("########### mapScanKey2Multi #################");
            ITC_KEYBOARD.CUSBkeys _cusb = new ITC_KEYBOARD.CUSBkeys();
            ITC_KEYBOARD.CUSBkeys.usbKeyStruct _usbKey = new CUSBkeys.usbKeyStruct();
            int iIdx = _cusb.getKeyStruct(0, CUsbKeyTypes.HWkeys.SCAN_Button_KeyLang1, ref _usbKey);
            if (iIdx != -1)
            {
                _usbKey.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
                _usbKey.bFlagMid = CUsbKeyTypes.usbFlagsMid.NoRepeat | CUsbKeyTypes.usbFlagsMid.Silent;//
                _usbKey.bFlagLow = CUsbKeyTypes.usbFlagsLow.MultiKeyIndex;
                _usbKey.bIntScan = b;

                addLog("scanbutton key index is " + iIdx.ToString());
                //_usbKey.bFlagHigh = CUsbKeyTypes.usbFlagsHigh.NoFlag;
                //_usbKey.bFlagMid = CUsbKeyTypes.usbFlagsMid.NOOP;
                //_usbKey.bFlagLow = CUsbKeyTypes.usbFlagsLow.NormalKey;
                for (int iPlane = 0; iPlane < _cusb.getNumPlanes(); iPlane++)
                {
                    addLog("using plane: " + iPlane.ToString());
                    if (_cusb.setKey(iPlane, _usbKey.bScanKey, _usbKey) == 0) //changed "setKey(0," to "setKey(i,"
                        addLog("setKey for scanbutton key OK");
                    else
                        addLog("setKey for scanbutton key failed");
                }
                _cusb.writeKeyTables();
                mapAllSide2NOOP();
            }
            else
            {
                addLog("Could not get index for scanbutton key");
            }
            addLog("----------- mapScanKey2Multi -----------------");
        }
    }
}
