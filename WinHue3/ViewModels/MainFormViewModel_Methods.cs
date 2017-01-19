﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using HueLib2;
using Action = HueLib2.Action;

namespace WinHue3.ViewModels
{
    public partial class MainFormViewModel : ValidatableBindableBase
    {

        private void ChangeBridgeSettings()
        {
            throw new NotImplementedException();
        }

        private bool DoBridgePairing(ObservableCollection<Bridge> listBridges = null)
        {
            Form_BridgeDetectionPairing dp = new Form_BridgeDetectionPairing(listBridges) { Owner = Application.Current.MainWindow };

            bool result = (bool)dp.ShowDialog();
            if (result)
            {
                ListBridges = dp.ViewModel.ListBridges;
                SaveSettings();
            }
            return result;
        }

        private void RefreshView()
        {
            Cursor_Tools.ShowWaitCursor();
            SelectedObject = null;
            log.Info($"Getting list of objects from bridge at {SelectedBridge.IpAddress}.");
            HelperResult hr = HueObjectHelper.GetBridgeDataStore(SelectedBridge);
            if (hr.Success)
            {
               /* ObservableCollection<HueObject> newbo =
                    new ObservableCollection<HueObject>((List<HueObject>) hr.Hrobject);
                var sorted = from item in newbo orderby item.GetName() select item;
                ListBridgeObjects = new ObservableCollection<HueObject>(sorted);*/

                ListBridgeObjects = new ObservableCollection<HueObject>((List<HueObject>)hr.Hrobject);

                log.Info($"Found {ListBridgeObjects.Count} objects in the bridge.");
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ListBridgeObjects);
                view.GroupDescriptions?.Clear();
                PropertyGroupDescription groupDesc = new TypeGroupDescription();
                view.GroupDescriptions?.Add(groupDesc);
                log.Info($"Finished refreshing view.");
            }
            else
            {
                ListBridgeObjects = null;
                log.Error(hr.Hrobject);
            }
            Cursor_Tools.ShowNormalCursor();
        }

        private void DoubleClickObject()
        {
            log.Debug("Double click on : " + _selectedObject);
            if ((_selectedObject is Light) || (_selectedObject is Group))
            {
                HelperResult hr = HueObjectHelper.ToggleObjectOnOffState(SelectedBridge, _selectedObject);
                if (hr.Success)
                {
                    ImageSource newimg = (ImageSource)hr.Hrobject;
                    if (newimg == null) return;
                    _selectedObject.Image = newimg;
                    int index = _listBridgeObjects.FindIndex(x => x.Id == _selectedObject.Id && x.GetType() == _selectedObject.GetType());
                    if (index == -1) return;
                    if (_selectedObject.HasProperty("state"))
                    {
                        ((Light)_selectedObject).state.on = !((Light)_selectedObject).state.on;
                        ((Light)_listBridgeObjects[index]).state.on = !((Light)_listBridgeObjects[index]).state.on;
                    }
                    else
                    {
                        ((Group)_selectedObject).action.on = !((Group)_selectedObject).action.on;
                        ((Group)_listBridgeObjects[index]).action.on = !((Group)_listBridgeObjects[index]).action.on;
                    }

                    _listBridgeObjects[index].Image = newimg;
                    OnPropertyChanged("SelectedObject");
                }
            }
            else
            {
                log.Info($"Activating scene : {_selectedObject.Id}");
                SelectedBridge.ActivateScene(_selectedObject.Id);

            }

        }

        private void RefreshObject(HueObject obj, bool logging = false)
        {
            int index = _listBridgeObjects.FindIndex(x => x.Id == obj.Id && x.GetType() == obj.GetType());
            if (index == -1) return;

            MethodInfo mi = typeof(HueObjectHelper).GetMethod("GetObject");
            MethodInfo generic = mi.MakeGenericMethod(obj.GetType());
            HelperResult hr = (HelperResult)generic.Invoke(SelectedBridge, new object[] { SelectedBridge, obj.Id });

            if (!hr.Success) return;
            HueObject newobj = (HueObject)hr.Hrobject;
            _listBridgeObjects[index].Image = newobj.Image;
            PropertyInfo[] pi = newobj.GetType().GetProperties();
            foreach (PropertyInfo p in pi)
            {
                if (_listBridgeObjects[index].HasProperty(p.Name))
                    p.SetValue(_listBridgeObjects[index], _listBridgeObjects[index].GetType().GetProperty(p.Name).GetValue(newobj));
            }
        }

        private void EditObject()
        {
            log.Debug("Editing object : " + _selectedObject);

            if (_selectedObject is Group)
            {
                Form_GroupCreator fgc = new Form_GroupCreator(SelectedObject,SelectedBridge) { Owner = Application.Current.MainWindow };
                if (fgc.ShowDialog() == true)
                {
                    RefreshObject(_selectedObject);
                }
            }
            else if (_selectedObject is Schedule)
            {
                Form_ScheduleCreator fsc = new Form_ScheduleCreator(SelectedObject,SelectedBridge) { Owner = Application.Current.MainWindow };
                if (fsc.ShowDialog() == true)
                {
                    RefreshObject(_selectedObject);
                }
            }
            else if (_selectedObject is Sensor)
            {
                Sensor obj = (Sensor)_selectedObject;
                switch (obj.modelid)
                {
                    case "PHDL00":
                        CommandResult cr = SelectedBridge.GetObject<Sensor>(obj.Id);
                        if (cr.Success)
                        {
                            Sensor daylight = (Sensor)cr.resultobject;
                            daylight.Id = obj.Id;
                            Form_Daylight dl = new Form_Daylight(daylight,SelectedBridge) { Owner = Application.Current.MainWindow };
                            if (dl.ShowDialog() == true)
                            {
                                RefreshObject(_selectedObject);
                            }

                        }

                        break;
                    case "ZGPSWITCH":
                        Form_HueTapConfig htc = new Form_HueTapConfig(obj.Id,SelectedBridge)
                        {
                            Owner = Application.Current.MainWindow
                        };
                        if (htc.ShowDialog() == true)
                        {
                            RefreshObject(_selectedObject);
                        }
                        break;
                    default:
                        CommandResult crs = SelectedBridge.GetObject<Sensor>(obj.Id);
                        if (crs.Success)
                        {
                            Form_SensorCreator fsc = new Form_SensorCreator(SelectedBridge,(Sensor)crs.resultobject)
                            {
                                Owner = Application.Current.MainWindow
                            };
                            if (fsc.ShowDialog() == true)
                            {
                                RefreshObject(_selectedObject);
                            }

                        }
                        break;
                }
            }
            else if (_selectedObject is Rule)
            {
                Form_RulesCreator2 frc = new Form_RulesCreator2(SelectedBridge, _selectedObject) { Owner = Application.Current.MainWindow };
                if (frc.ShowDialog() == true)
                {
                    RefreshObject(_selectedObject);
                }
            }
            else if (_selectedObject is Scene)
            {
                Form_SceneCreator fscc = new Form_SceneCreator(SelectedBridge, _selectedObject) { Owner = Application.Current.MainWindow };
                if (fscc.ShowDialog() == true)
                {
                    RefreshObject(_selectedObject);
                }
            }
            else if (_selectedObject is Resourcelink)
            {
                Form_ResourceLinksCreator frlc = new Form_ResourceLinksCreator(SelectedBridge,(Resourcelink)_selectedObject) { Owner = Application.Current.MainWindow };

                if (frlc.ShowDialog() == true)
                {
                    RefreshObject(_selectedObject);
                }

            }
        }

        private void IdentifyLong()
        {
            MethodInfo mi = typeof(Bridge).GetMethod("SetState");
            MethodInfo generic = mi.MakeGenericMethod(_selectedObject.GetType());
            log.Info($@"Sending the long Identify command to object ID : {_selectedObject.Id}");
            CommandResult hr = (CommandResult)generic.Invoke(SelectedBridge, new object[] { new CommonProperties() { alert = "lselect" }, _selectedObject.Id });
        }

        private void IdentifyShort()
        {
            MethodInfo mi = typeof(Bridge).GetMethod("SetState");
            MethodInfo generic = mi.MakeGenericMethod(_selectedObject.GetType());
            log.Info($@"Sending the long Identify command to object ID : {_selectedObject.Id}");
            CommandResult hr = (CommandResult)generic.Invoke(SelectedBridge, new object[] { new CommonProperties() { alert = "select" }, _selectedObject.Id });
        }

        public bool SaveSettings()
        {
            foreach (Bridge br in ListBridges)
            {
                if (WinHueSettings.settings.BridgeInfo.ContainsKey(br.Mac))
                    WinHueSettings.settings.BridgeInfo[br.Mac] = new BridgeSaveSettings()
                    {
                        ip = br.IpAddress.ToString(),
                        apikey = br.ApiKey,
                        apiversion = br.ApiVersion,
                        swversion = br.SwVersion,
                        name = br.Name
                    };
                else
                    WinHueSettings.settings.BridgeInfo.Add(br.Mac,
                        new BridgeSaveSettings() { ip = br.IpAddress.ToString(), apikey = br.ApiKey, apiversion = br.ApiVersion, swversion = br.SwVersion, name = br.Name });

                if (br.IsDefault) WinHueSettings.settings.DefaultBridge = br.Mac;
            }

            return WinHueSettings.Save();
        }

        private void ShowEventLog()
        {
            if (_eventlogform.IsVisible) return;
            log.Debug("Opening event log.");
            _eventlogform.Show();

        }

        private void AllOn()
        {
            log.Info("Sending all on command to bridge" + SelectedBridge.IpAddress);
            HueLib2.Action act = new HueLib2.Action() { @on = true };
            if (WinHueSettings.settings.AllOnTT != null) act.transitiontime = WinHueSettings.settings.AllOnTT;
            CommandResult bresult = SelectedBridge.SetState<Group>(act, "0");
            if (!bresult.Success) return;
            log.Debug("Refreshing the main view.");
            RefreshView();
        }

        private void AllOff()
        {
            log.Info("Sending all off command to bridge" + SelectedBridge.IpAddress);
            HueLib2.Action act = new Action() { @on = false };
            if (WinHueSettings.settings.AllOnTT != null) act.transitiontime = WinHueSettings.settings.AllOnTT;
            CommandResult bresult = SelectedBridge.SetState<Group>(act, "0");
            if (!bresult.Success) return;
            log.Debug("Refreshing the main view.");
            RefreshView();
        }

        private void CheckForNewBulb()
        {
            if (SelectedBridge == null) return;
            CommandResult bresult = SelectedBridge.FindNewObjects<Light>();
            if (bresult.Success)
            {
                log.Info("Seaching for new lights...");
                _findlighttimer.Start();
                OnPropertyChanged("EnableSearchLights");
            }
            else
            {
                MessageBox.Show(GlobalStrings.Error_Getting_NewLights, GlobalStrings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                log.Error("There was an error while looking for new lights.");
            }
        }

        private void CreateGroup()
        {
            Form_GroupCreator fgc = new Form_GroupCreator(SelectedBridge) { Owner = Application.Current.MainWindow };
            log.Debug($@"Opening the Group creator window for bridge {SelectedBridge.IpAddress} ");
            if (fgc.ShowDialog() != true) return;
            if (fgc.GetCreatedOrModifiedID() == null) return;

            HelperResult hr = HueObjectHelper.GetObject<Group>(SelectedBridge, fgc.GetCreatedOrModifiedID());
            if (hr.Success)
            {              
                _listBridgeObjects.Add((HueObject)hr.Hrobject);
            }
            else
            {
                MessageBoxError.ShowLastErrorMessages(SelectedBridge);
            }
        }

        private void CreateScene()
        {
            Form_SceneCreator fsc = new Form_SceneCreator(SelectedBridge) { Owner = Application.Current.MainWindow };
            log.Debug($@"Opening the scene creator window for bridge {SelectedBridge.IpAddress} ");
            if (fsc.ShowDialog() != true) return;
            log.Debug($@"Getting the newly created scene ID {fsc.GetCreatedOrModifiedID()} from bridge {SelectedBridge.IpAddress}");
            HelperResult hr = HueObjectHelper.GetObject<Scene>(SelectedBridge, fsc.GetCreatedOrModifiedID());
            if (hr.Success)
            {
                _listBridgeObjects.Add((HueObject)hr.Hrobject);
            }
            else
            {
                MessageBoxError.ShowLastErrorMessages(SelectedBridge);
            }
        }

        private void CreateSchedule()
        {
            Form_ScheduleCreator fscc = new Form_ScheduleCreator(_selectedObject,SelectedBridge) { Owner = Application.Current.MainWindow };
            log.Debug($@"Opening the schedule creator window passing bridge {SelectedBridge.IpAddress} ");
            if (fscc.ShowDialog() != true) return;
            log.Debug($@"Getting the newly created schedule ID {fscc.GetCreatedOrModifiedID()} from bridge {SelectedBridge.IpAddress}");
            HelperResult hr = HueObjectHelper.GetObject<Schedule>(SelectedBridge, fscc.GetCreatedOrModifiedID());
            if (hr.Success)
            {
                _listBridgeObjects.Add((HueObject)hr.Hrobject);
            }
            else
            {
                MessageBoxError.ShowLastErrorMessages(SelectedBridge);
            }

        }

        private void CreateRule()
        {
            Form_RulesCreator2 frc = new Form_RulesCreator2(SelectedBridge) { Owner = Application.Current.MainWindow };
            log.Debug($@"Opening the rule creator window passing bridge {SelectedBridge.IpAddress} ");
            if (frc.ShowDialog() != true) return;
            log.Debug($@"Getting the newly created rule ID {frc.GetCreatedOrModifiedId()} from bridge {SelectedBridge.IpAddress}");
            HelperResult hr = HueObjectHelper.GetObject<Rule>(SelectedBridge, frc.GetCreatedOrModifiedId());
            if (hr.Success)
            {

                _listBridgeObjects.Add((HueObject)hr.Hrobject);
            }
            else
            {
                MessageBoxError.ShowLastErrorMessages(SelectedBridge);
            }

        }

        private void CreateSensor()
        {
            Form_SensorCreator fsc = new Form_SensorCreator(SelectedBridge) { Owner = Application.Current.MainWindow };
            log.Debug($@"Opening the sensor creator window passing bridge {SelectedBridge.IpAddress} ");
            if (fsc.ShowDialog() != true) return;
            log.Debug($@"Getting the newly created sensor ID {fsc.GetCreatedOrModifiedID()} from bridge {SelectedBridge.IpAddress}");
            HelperResult hr = HueObjectHelper.GetObject<Sensor>(SelectedBridge, fsc.GetCreatedOrModifiedID());
            if (hr.Success)
            {

                _listBridgeObjects.Add((HueObject)hr.Hrobject);
            }
            else
            {
                MessageBoxError.ShowLastErrorMessages(SelectedBridge);
            }
        }

        private void SearchNewSensors()
        {
            if (SelectedBridge == null) return;
            CommandResult bresult = SelectedBridge.FindNewObjects<Sensor>();
            if (bresult.Success)
            {
                log.Info("Looking for new sensors for 1 minute.");
                _findsensortimer.Start();
            }
            else
            {
                log.Error("Unable to look for new sensors. Please check the log for more details.");
            }
        }

        public void CreateHotKey()
        {
            Form_HotKeyCreator fhkc = new Form_HotKeyCreator(SelectedBridge) { Owner = Application.Current.MainWindow };
            fhkc.ShowDialog();
            _listHotKeys = fhkc.GetHotKeys();
            LoadHotkeys();
        }


        private void CreateResourceLink()
        {
            Form_ResourceLinksCreator frc = new Form_ResourceLinksCreator(SelectedBridge) { Owner = Application.Current.MainWindow };
            if (frc.ShowDialog() == true)
            {

            }

        }

        private void DoBridgeUpdate()
        {
            if (SelectedBridge == null) return;
            if (MessageBox.Show(GlobalStrings.Update_Confirmation, GlobalStrings.Warning, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            log.Info("Updating bridge to the latest firmware.");
            CommandResult bresult = SelectedBridge.DoSwUpdate();
            if (!bresult.Success)
            {
                log.Error("An error occured while trying to start a bridge update. Please try again later.");
                return;
            }
            else
            {
                Form_Wait fw = new Form_Wait();
                fw.ShowWait(GlobalStrings.ApplyingUpdate, new TimeSpan(0, 0, 0, 180), Application.Current.MainWindow);
                CommandResult bsettings = SelectedBridge.GetBridgeSettings();
                if (bsettings.Success)
                {
                    BridgeSettings brs = (BridgeSettings)bsettings.resultobject;

                    switch (brs.swupdate.updatestate)
                    {
                        case 0:
                            log.Info("Bridge updated succesfully");
                            SelectedBridge.SetNotify();
                            break;
                        case 1:
                            log.Info("Bridge update incoming. Please wait for it to be ready.");
                            break;
                        case 2:
                            log.Info("Bridge update still available or a new update is available.");
                            break;
                        case null:
                            log.Error("Bride update state was null.");
                            break;
                        default:
                            log.Error("An error occured while updating the bridge. Please try again later.");
                            break;

                    }

                }
            }

            OnPropertyChanged("UpdateAvailable");
        }

        public void HandleHotkey(HotKeyHandle e)
        {

            ModifierKeys m = e.KeyModifiers;
            Key k = e.Key;
            try
            {

                HotKey h = _listHotKeys.First(x => x.Modifier == m && x.Key == k);
                if (!(h.objecType == null && _selectedObject == null))
                {
                    Type objtype = h.objecType == null ? _selectedObject.GetType() : h.objecType;

                    if (objtype == typeof(Scene))
                    {
                        SelectedBridge.ActivateScene(h.id);
                    }
                    else if (objtype == typeof(Group) || objtype == typeof(Light))
                    {

                        List<object> listparams = new List<object>();
                        listparams.Add(Convert.ChangeType(h.properties, typeof(CommonProperties)));
                        listparams.Add(h.objecType != null ? h.id : _selectedObject.Id);
                        ExecuteGenericMethod<CommandResult>(SelectedBridge, "SetState", listparams.ToArray());
                    }
                    else
                    {
                        log.Warn($"Type of object {objtype} not supported");
                    }
                }
                else
                {
                    log.Warn("You must select an object in WinHue in order to apply this hotkey.");
                }
            }
            catch (InvalidOperationException)
            {
                log.Warn("No Hotkey was found.");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void LoadHotkeys()
        {

            if (_lhk.Count > 0)
            {
                while (_lhk.Count != 0)
                {
                    _lhk[0].Unregister();
                    _lhk.Remove(_lhk[0]);
                }
            }

            foreach (HotKey h in _listHotKeys)
            {
                HotKeyHandle hkh = new HotKeyHandle(h, HandleHotkey);
                if (hkh.Register())
                    _lhk.Add(hkh);
                else
                    log.Error($"Cannot register hotkey {h.Name} key seems to be already taken by another process.");
            }

        }

        private void SliderChangeHue()
        {
            if (_selectedObject == null || SelectedBridge == null) return;
            ExecuteGenericMethod<CommandResult>(SelectedBridge, "SetState", new object[] { new CommonProperties() { hue = MainFormModel.SliderHue, transitiontime = _transitiontime }, _selectedObject.Id });
        }

        private T ExecuteGenericMethod<T>(object objectmethod, string methodname, object[] paramsarray) where T : new()
        {
            MethodInfo mi = objectmethod.GetType().GetMethod(methodname);
            MethodInfo generic = mi.MakeGenericMethod(_selectedObject.GetType());
            object result = generic.Invoke(SelectedBridge, paramsarray);
            return (T)result;
        }

        private void SliderChangeBri()
        {
            if (_selectedObject == null || SelectedBridge == null) return;
            ExecuteGenericMethod<CommandResult>(SelectedBridge, "SetState", new object[] { new CommonProperties() { bri = MainFormModel.SliderBri, transitiontime = _transitiontime }, _selectedObject.Id });
        }

        private void SliderChangeCt()
        {
            if (_selectedObject == null || SelectedBridge == null) return;
            ExecuteGenericMethod<CommandResult>(SelectedBridge, "SetState", new object[] { new CommonProperties() { ct = MainFormModel.SliderCt, transitiontime = _transitiontime }, _selectedObject.Id });
        }

        private void SliderChangeSat()
        {
            if (_selectedObject == null || SelectedBridge == null) return;
            ExecuteGenericMethod<CommandResult>(SelectedBridge, "SetState", new object[] { new CommonProperties() { sat = MainFormModel.SliderSat, transitiontime = _transitiontime }, _selectedObject.Id });
        }

        private void SliderChangeXy()
        {
            if (_selectedObject == null || SelectedBridge == null) return;
            ExecuteGenericMethod<CommandResult>(SelectedBridge, "SetState", new object[] { new CommonProperties() { xy = new XY() { x = MainFormModel.SliderX, y = MainFormModel.SliderY}, transitiontime = _transitiontime }, _selectedObject.Id });

        }

        private void Sensitivity(int sensivity)
        {
            Sensor sensor = (Sensor)SelectedObject;
            ((HueMotionSensorConfig)sensor.config).sensitivity = sensivity;
            // sensor.Id = _selectedObject.Id;
            sensor.type = ((Sensor)SelectedObject).type;
            SelectedBridge.ModifyObject<Sensor>(sensor, sensor.Id);
        }

        private void DeleteObject()
        {
            if (
                MessageBox.Show(string.Format(GlobalStrings.Confirm_Delete_Object, _selectedObject.GetName()),
                    GlobalStrings.Warning, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            MethodInfo method = typeof(Bridge).GetMethod("RemoveObject");
            MethodInfo generic = method.MakeGenericMethod(_selectedObject.GetType());
            CommandResult bresult = (CommandResult)generic.Invoke(SelectedBridge, new object[] { SelectedObject.Id });

            log.Debug("Result : " + bresult.resultobject);
            if (bresult.Success)
            {
                int index =
                    ListBridgeObjects.FindIndex(
                        x => x.Id == SelectedObject.Id && x.GetType() == SelectedObject.GetType());
                if (index == -1) return;
                ListBridgeObjects.RemoveAt(index);
                OnPropertyChanged("ListBridgeObjects");
                SelectedObject = null;
                log.Info($"Object ID : {index} removed.");
            }
            else
            {
                MessageBoxError.ShowLastErrorMessages(SelectedBridge);
            }
        }

        private void RenameObject()
        {
            Form_RenameObject fro = new Form_RenameObject(SelectedBridge, SelectedObject) { Owner = Application.Current.MainWindow };
            if (fro.ShowDialog() != true) return;
            SelectedObject.SetName(fro.GetNewName());
            int index = ListBridgeObjects.FindIndex(x => x.Id == SelectedObject.Id && x.GetType() == SelectedObject.GetType());
            if (index == -1) return;
            ListBridgeObjects[index].SetName(fro.GetNewName());
            log.Info($"Renamed object ID : {index} renamed.");

        }

        private void DuplicateObject()
        {
            
        }
    }
}