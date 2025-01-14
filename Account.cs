﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using FreeSWITCH.Native;
using System.Xml.Serialization;
namespace FSClient {
	[XmlRoot("settingsAccounts")]
	public class SettingsAccounts {
		[XmlElement("settingsAccount")]
		public SettingsAccount[] accounts { get; set; }
	}
	public class SettingsAccount {
		public bool enabled { get; set; }
		public bool is_default_account { get; set; }
		public SettingsField[] fields { get; set; }
		public Account GetAccount() {
			Account act = new Account { enabled = enabled, is_default_account = is_default_account };
			foreach (SettingsField field in fields) {
				FieldValue val = FieldValue.GetByName(act.values, field.name);
				if (val != null)
					val.value = field.value;
			}
			return act;
		}
		public SettingsAccount() {

		}
		public SettingsAccount(Account account) {
			enabled = account.enabled;
			is_default_account = account.is_default_account;
			fields = (from fv in account.values select new SettingsField(fv)).ToArray();
		}
	}
	public class Account : ObservableClass {
		public static ObservableCollection<Account> accounts = new ObservableCollection<Account>();

		private static Account current_editing_account;
		public static Account default_account;
		public static Field[] fields = {
									new	Field(Field.FIELD_TYPE.String, "Name","name","","",""),
									new	Field(Field.FIELD_TYPE.Combo, "Account Number","guid","","1","","0","1","2","3","4","5","6","7","8","9"),
									new	Field(Field.FIELD_TYPE.String, "Server","server","realm","",""),
  									new Field(Field.FIELD_TYPE.String,"Username", "username","username","",""),
									new Field(Field.FIELD_TYPE.Password,"Password", "password","password","",""),
									new	Field(Field.FIELD_TYPE.String, "Caller ID Name","caller_id_name","","",""),
									new	Field(Field.FIELD_TYPE.String, "Caller ID Number","caller_id_number","","",""),
                                    new Field(Field.FIELD_TYPE.Bool,"Use extension in contact","extension_in_contact","extension_in_contact","false",""),
                                    new	Field(Field.FIELD_TYPE.String, "SIP URL for Checking Voicemail","sip_check_voicemail_url","","",""),
									new	Field(Field.FIELD_TYPE.String, "SIP URL for Sending to Voicemail","sip_send_voicemail_url","","",""),
									new Field(Field.FIELD_TYPE.Bool,"Register","register","register","true",""),
									new Field(Field.FIELD_TYPE.Combo,"Register Transport","register-transport","register-transport","udp","", "udp","tcp","sctp","tls"),
									new Field(Field.FIELD_TYPE.Bool,"SIP Secure Media","sip_secure_media","sip_secure_media","false",""),

									new Field(Field.FIELD_TYPE.String,"Proxy","proxy","proxy","","Advanced"),
									new Field(Field.FIELD_TYPE.Int,"Ping Seconds","ping","ping","30","Advanced"),
									new Field(Field.FIELD_TYPE.String,"Outbound Proxy","outbound-proxy","outbound-proxy","","Advanced"),
									new Field(Field.FIELD_TYPE.String,"Register Proxy","register-proxy","register-proxy","","Advanced"),
									new Field(Field.FIELD_TYPE.Int,"Expire Seconds","expire-seconds","expire-seconds","3600","Advanced"),
									new Field(Field.FIELD_TYPE.Int,"Retry Seconds","retry-seconds","retry-seconds","30","Advanced"),
									new Field(Field.FIELD_TYPE.String,"Contact Params","contact-params","contact-params","","Advanced")

								  };
		#region Static Methods
		private static string[] AllowedEmptyFields = new string[] { };
		private static void callback(object state) {
			Broker.get_instance().reload_sofia(Sofia.RELOAD_CONFIG_MODE.SOFT);
		}
		public static void ReloadAccounts() {
			foreach (Account account in accounts)
				account.KillGateway();

			ReloadSofia();
		}
		private static void ReloadSofia() {
			new System.Threading.Timer(callback, null, 2500, System.Threading.Timeout.Infinite);
		}

		public static void SaveSettings() {
			SettingsAccounts accts = new SettingsAccounts();
			accts.accounts = (from a in accounts select new SettingsAccount(a)).ToArray();
			Properties.Settings.Default.Accounts = accts;
		}
		private static bool have_inited;
		private static string ValidateGuid(String guid) {
			var it = (from a in accounts where a.guid == guid && a != current_editing_account select a).SingleOrDefault();
			if (it == null)
				return null;

			return "Invalid Account Number, Already in use";
		}
		public static void LoadSettings() {
			SettingsAccounts accts = Properties.Settings.Default.Accounts;
			if (accts == null || accts.accounts == null)
				return;
			accounts.Clear();
			if (have_inited == false) {
				have_inited = true;
				accounts.CollectionChanged += accounts_CollectionChanged;
				Field field = Field.GetByName(fields, "guid");
				field.Validator = new Field.validate_field_del(ValidateGuid);

			}
			foreach (SettingsAccount account in accts.accounts) {
				Account acct = account.GetAccount();
				AddAccount(acct);
			}
		}
		private static void ensure_default_account() {
			if (default_account != null && default_account.enabled)
				return;
			var res = (from a in accounts where a.enabled orderby a.gateway_id select a).FirstOrDefault();
			if (res != null)
				res.is_default_account = true;
			else
				default_account = null;
		}
		static void accounts_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
			ensure_default_account();
		}
		public static void AddAccount(Account account) {
			accounts.Add(account);
			bool was_default = account.is_default_account;
			account.is_default_account = false;
			account.is_default_account = was_default;
			String old_guid = account.guid;
			account.guid = "";
			account.guid = old_guid;
		}
		public static void RemoveAccount(Account account) {
			account.KillGateway();
			account.is_default_account = false;
			accounts.Remove(account);
			ReloadSofia();
		}

		public static void create_gateway_nodes(XmlNode gateways_node, bool tls_enabled, bool tls_only) {
			foreach (Account account in accounts) {
				if (account.enabled)
					account.create_gateway_node(gateways_node, tls_enabled, tls_only);
			}
		}
		public static void HandleGatewayEvent(FSEvent evt) {
			String gateway = evt.get_header("Gateway");
			Account account = (from a in accounts where a.gateway_id == gateway select a).SingleOrDefault();
			if (account == null)
				return;
			account.state = evt.get_header("State");
		}
		public static void NewEvent(object sender, FSEvent evt) {
			switch (evt.event_id) {
				case switch_event_types_t.SWITCH_EVENT_CUSTOM:
					if (evt.subclass_name == "sofia::gateway_state")
						HandleGatewayEvent(evt);
					break;
			}
		}
		#endregion
		FieldValue _username;
		FieldValue _server;
		FieldValue _caller_id_name;
		FieldValue _caller_id_number;
		FieldValue _guid;
		FieldValue _name;
		private static bool guid_ok(Account acct, String guid) {
			var other = (from a in accounts where a.guid == guid && a != acct select a).SingleOrDefault();
			return (other == null);
		}
		public Account() {
			_server = FieldValue.GetByName(values, "server");
			_server.PropertyChanged += (s, e) => RaisePropertyChanged("server");
			_username = FieldValue.GetByName(values, "username");
			_username.PropertyChanged += (s, e) => RaisePropertyChanged("username");
			_name = FieldValue.GetByName(values, "name");
			_name.PropertyChanged += (s, e) => RaisePropertyChanged("name");
			_guid = FieldValue.GetByName(values, "guid");
			_guid.PropertyChanged += (s, e) => {

				if (!guid_ok(this, _guid.value) || String.IsNullOrEmpty(_guid.value)) {
					for (int i = 1; i <= 10; i++) {
						int val = i == 10 ? 0 : i; // want 0 checked last.
						if (guid_ok(this, val.ToString())) {
							_guid.value = val.ToString();
							return;
						}
					}
				}
				RaisePropertyChanged("guid");
				RaisePropertyChanged("gateway_id");
			};
			_caller_id_name = FieldValue.GetByName(values, "caller_id_name");
			_caller_id_name.PropertyChanged += (s, e) => RaisePropertyChanged("caller_id_name");
			_caller_id_number = FieldValue.GetByName(values, "caller_id_number");
			_caller_id_number.PropertyChanged += (s, e) => RaisePropertyChanged("caller_id_number");
			_guid.value = "1";
			PropertyChanged += Account_PropertyChanged;
			state = "NOREG";
		}
		private static Regex sip_regex;
		public void CheckVoicemail(){
			String url = getCheckVoicemailURL();
			if (String.IsNullOrWhiteSpace(url)){
				MessageBox.Show("No url specified to check voicemail, edit account first.");
				return;
			}
			CreateCall(getCheckVoicemailURL());
		}
		public void CreateCall(String number) {
			if (!enabled)
				return;
			Broker.get_instance().BringToFront(false);
			String var_str = "origination_caller_id_name='" + caller_id_name + "',origination_caller_id_number='" + caller_id_number + "'";
			if (secure_media)
				var_str += ",sip_secure_media=true";

			if (Broker.get_instance().DirectSipDial){
				if (sip_regex == null)
					sip_regex = new Regex(@"^sip:(.+)$",RegexOptions.Compiled);
				Match match = sip_regex.Match(number);
				if (match.Success){
					string sip_uri = match.Groups[1].Value;
					PortAudio.Call("{" + var_str + ",gw_ref='" + gateway_id + "'}sofia/softphone/" + sip_uri);
					return;
				}
			}
			PortAudio.Call("{" + var_str + "}sofia/gateway/" + gateway_id + "/" + number);
		}
		private void KillGateway() {
			if (!String.IsNullOrEmpty(old_gateway_id))
				Utils.api_exec("sofia", "profile softphone killgw " + old_gateway_id);
		}
		FieldValue check_voicemail_val;
		public string getCheckVoicemailURL() {
			if (check_voicemail_val == null)
				check_voicemail_val = FieldValue.GetByName(values, "sip_check_voicemail_url");
			return check_voicemail_val.value;

		}
		FieldValue send_voicemail_val;
		public string getSendVoicemailURL() {
			if (send_voicemail_val == null)
				send_voicemail_val = FieldValue.GetByName(values, "sip_send_voicemail_url");
			return send_voicemail_val.value;

		}
		public override string ToString() {
			return name;
		}
		private void Account_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
			if (e.PropertyName == "is_default_account") {
				if (!is_default_account) {
					DelayedFunction.DelayedCall("Account_ensure_default", ensure_default_account, 500);
					return;
				}
				foreach (Account account in accounts)
					if (account != this)
						account.is_default_account = false;
				default_account = this;
			}
		}

		#region Properties

		public string username {
			get { return _username.value; }
		}
		public string server {
			get { return _server.value; }
		}

		public bool is_default_account {
			get { return _is_default_account; }
			set {
				if (value == _is_default_account)
					return;
				_is_default_account = value;
				RaisePropertyChanged("is_default_account");
			}
		}
		private bool _is_default_account;

		public bool enabled {
			get { return _enabled; }
			set {
				if (value == _enabled)
					return;
				_enabled = value;
				if (!enabled)
					is_default_account = false;
				else
					ensure_default_account();

				RaisePropertyChanged("enabled");
			}
		}
		private bool _enabled;

		public bool secure_media {
			get { return _secure_media; }
			private set {
				if (value == _secure_media)
					return;
				_secure_media = value;
				RaisePropertyChanged("secure_media");
			}
		}
		private bool _secure_media;

		public string name {
			get { return _name.value; }
		}


		public string guid {
			get { return _guid.value; }
			set { _guid.value = value; }
		}


		public string caller_id_name {
			get { return _caller_id_name.value; }
		}


		public string caller_id_number {
			get { return _caller_id_number.value; }
		}


		public string state {
			get { return _state; }
			set {
				if (value == _state)
					return;
				_state = value;
				if (enabled && state == "NOREG") //need to make sure it became active, sometimes the rescan fails
					DelayedFunction.DelayedCall("ACCTSTATUSCHECK" + gateway_id, acct_status_check, 4000);
				RaisePropertyChanged("state");
			}
		}
		private string _state;

		public string gateway_id {
			get { return guid; }
		}

		public FieldValue[] values = FieldValue.FieldValues(fields);

		#endregion
		private string old_gateway_id;
		private void acct_status_check() {
			if (enabled && state == "NOREG")
				Broker.get_instance().reload_sofia(Sofia.RELOAD_CONFIG_MODE.SOFT);

		}
		private void create_gateway_node(XmlNode gateways_node, bool tls_enabled, bool tls_only) {
			XmlNode node = XmlUtils.AddNodeNode(gateways_node, "gateway");
			XmlUtils.AddNodeAttrib(node, "name", gateway_id);
			old_gateway_id = gateway_id;
			foreach (FieldValue value in values) {
				if (String.IsNullOrEmpty(value.field.xml_name))
					continue;
				if (String.IsNullOrWhiteSpace(value.value) && !AllowedEmptyFields.Contains(value.field.name))
					continue;
				if (value.field.name == "register-transport") {
					if (value.value == "tls" && !tls_enabled){
						MessageBox.Show("Warning the register-transport for account: " + name + " is set to tls, however you have tls disabled in your sofia settings this account will be disabled for now");
						gateways_node.RemoveChild(node);
						enabled = false;
						return;
					}
					if(value.value != "tls" && tls_only){
						MessageBox.Show("Warning the register-transport for account: " + name + " is not set to tls, however you have tls only enabled in your sofia settings this account will be disabled for now");
						gateways_node.RemoveChild(node);
						enabled = false;
						return;
					}
				}
				if (value.field.name == "sip_secure_media")
					secure_media = value.value == "true";
                if(value.field.name == "extension_in_contact")
                {
                    if(value.value == "true")
                    {
                        FieldValue user = FieldValue.GetByName(values, "username");
                        Utils.add_xml_param(node, "extension-in-contact", user.value);
                    }
                    continue;
                }
				Utils.add_xml_param(node, value.field.xml_name, value.value);
			}
			//Was preventing gateway ID from being passed to create_channel so removed (needed for incoming calls)
			//FieldValue user = FieldValue.GetByName(values, "username");
			//Utils.add_xml_param(node, "extension-in-contact", user.value);
		}
		public void ReloadAccount() {
			KillGateway();
			ReloadSofia();
		}
		public bool edit() {
			current_editing_account = this;
			GenericEditor editor = new GenericEditor();
			List<FieldValue> vals = new List<FieldValue>(values);
			var enabled_field = new Field(Field.FIELD_TYPE.Bool, "Account Enabled","enabled","",enabled ? "true" : "false","");
			var enabled_val = enabled_field.GetDefaultValue();
			vals.Insert(0, enabled_val);
			editor.Init("Editing Account", vals);
			editor.ShowDialog();
			if (editor.DialogResult == true){
				if (enabled_val.value == "true")
					enabled = true;
				else
					enabled = false;
				ReloadAccount();
			}
			else
				return false;
			return true;
		}
	}
}
