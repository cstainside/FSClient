﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace FSClient {
	[XmlRoot("settingsSofia")]
	public class SettingsSofia {
		public SettingsField[] fields { get; set; }
		public Sofia GetSofia() {
			Sofia sofia = new Sofia();
			foreach (SettingsField field in fields) {
				FieldValue val = FieldValue.GetByName(sofia.values, field.name);
				if (val != null)
					val.value = field.value;
			}
			return sofia;
		}
		public SettingsSofia() {
		}
		public SettingsSofia(Sofia sofia) {
			fields = (from fv in sofia.values select new SettingsField(fv)).ToArray();
		}
	}

	public class Sofia {
		public static Field[] fields = {

										   /*Default*/
											new Field(Field.FIELD_TYPE.MultiItemSort,"Codec Preferences","codec-prefs","codec-prefs","OPUS,PCMU,PCMA,GSM","","OPUS","PCMA","PCMU","GSM","G722","G7221@16000h","G7221@32000h","AAL2-G726-16","AAL2-G726-24","AAL2-G726-32","AAL2-G726-40","BV16","BV32","DVI4@16000h@40i","DVI4@8000h@20i","G726-16","G726-24","G726-32","G726-40","L16","LPC","iLBC@30i","G729","isac","SILK"),
											new Field(Field.FIELD_TYPE.Combo,"Inbound Codec Negotiation","inbound-codec-negotiation","inbound-codec-negotiation","generous","","generous","greedy","scrooge"),
											new Field(Field.FIELD_TYPE.String,"External RTP IP","ext-rtp-ip","ext-rtp-ip","auto-nat",""),
											new Field(Field.FIELD_TYPE.String,"External SIP IP","ext-sip-ip","ext-sip-ip","auto-nat",""),
											new Field(Field.FIELD_TYPE.String,"RTP IP","rtp-ip","rtp-ip","auto",""),
											new Field(Field.FIELD_TYPE.String,"SIP IP","sip-ip","sip-ip","auto",""),
											new Field(Field.FIELD_TYPE.String,"Hold Music","hold-music","hold-music","local_stream://moh",""),
											new Field(Field.FIELD_TYPE.String,"User Agent","user-agent-string","user-agent-string","FreeSWITCH/FSClient",""),
											new Field(Field.FIELD_TYPE.Bool,"SIP Trace","sip-trace","sip-trace","false",""),
											new Field(Field.FIELD_TYPE.Combo,"Debug Level","debug","debug","0","","0","1","2","3","4","5","6","7","8","9"),
										   
											/*NAT*/
											new	Field(Field.FIELD_TYPE.Combo,"Apply Nat ACL","apply-nat-acl","apply-nat-acl","rfc1918","NAT",new Field.FieldOption{display_value="rfc1918", value="rfc1918"},new Field.FieldOption{display_value="None", value=""}),
											new Field(Field.FIELD_TYPE.Bool,"Agressive Nat Detection","aggressive-nat-detection","aggressive-nat-detection","true","NAT"),
                                            new Field(Field.FIELD_TYPE.Combo, "Force RPort", "NDLB-force-rport", "NDLB-force-rport", "false", "NAT", "false","safe","true"), 

											/*Security*/
											new Field(Field.FIELD_TYPE.Bool,"TLS","tls","tls","false","Security"),
											new Field(Field.FIELD_TYPE.Bool,"TLS Only","tls-only","tls-only","false","Security"),
											new Field(Field.FIELD_TYPE.Combo,"TLS Verify Policy","tls-verify-policy","tls-verify-policy","subjects_out|in","Security",new Field.FieldOption{display_value="None", value=""},new Field.FieldOption{display_value="Certs", value="all"},new Field.FieldOption{display_value="Certs & Hostnames", value="subjects_out|in"}),
											new Field(Field.FIELD_TYPE.Combo,"TLS Version","tls-version","tls-version","tlsv1","Security","tlsv1","sslv23"),
											new Field(Field.FIELD_TYPE.String,"TLS Bind Params","tls-bind-params","tls-bind-params","transport=tls","Security"),
											new Field(Field.FIELD_TYPE.Int,"TLS SIP Port","tls-sip-port","tls-sip-port","12347","Security"),
											new Field(Field.FIELD_TYPE.String,"TLS Certificate Directory","tls-cert-dir","tls-cert-dir","conf/ssl","Security"),
											new Field(Field.FIELD_TYPE.Int,"TLS Max Verify Depth","tls-verify-depth","tls-verify-depth","2","Security"),
											new Field(Field.FIELD_TYPE.Bool,"TLS Verify Date","tls-verify-date","tls-verify-date","true","Security"),

											/*Advanced*/
											new Field(Field.FIELD_TYPE.String,"Challenge Realm","challenge-realm","challenge-realm","auto_from","Advanced"),
											new Field(Field.FIELD_TYPE.Int,"SIP Port","sip-port","sip-port","12346","Advanced"),
											new Field(Field.FIELD_TYPE.Int,"DTMF Duration","dtmf-duration","dtmf-duration","2000","Advanced"),
											new Field(Field.FIELD_TYPE.Bool,"STUN Enabled","stun-enabled","stun-enabled","true","Advanced"),
											new Field(Field.FIELD_TYPE.Int,"Jitter Buffer","auto-jitterbuffer-msec","auto-jitterbuffer-msec","60","Advanced"),

											/*Advanced Less Important*/
											new Field(Field.FIELD_TYPE.Int,"Max Proceeding","max-proceeding","max-proceeding","3","Advanced"),
											new Field(Field.FIELD_TYPE.Int,"Sip Auth Nonce TTL","nonce-ttl","nonce-ttl","60","Advanced"),
											new Field(Field.FIELD_TYPE.Int,"rfc2833-pt","rfc2833-pt","rfc2833-pt","101","Advanced"),
											new Field(Field.FIELD_TYPE.Int,"RTP Hold Timeout Seconds","rtp-hold-timeout-sec","rtp-hold-timeout-sec","1800","Advanced"),
											new Field(Field.FIELD_TYPE.Int,"RTP Tiemout Seconds","rtp-timeout-sec","rtp-timeout-sec","300","Advanced"),
											new Field(Field.FIELD_TYPE.String,"RTP Timer Name","rtp-timer-name","rtp-timer-name","soft","Advanced"),
									   };

		private static string[] AllowedEmptyFields = new string[] { };
		public FieldValue[] values = FieldValue.FieldValues(fields);
		public void gen_config(XmlNode config_node) {

			XmlNode global_settings = XmlUtils.AddNodeNode(config_node, "global_settings");
			Utils.add_xml_param(global_settings, "auto-restart", "true");
			Utils.add_xml_param(global_settings, "log-level", "0");
			XmlNode profiles = XmlUtils.AddNodeNode(config_node, "profiles");
			XmlNode profile = XmlUtils.AddNodeNode(profiles, "profile");
			XmlUtils.AddNodeAttrib(profile, "name", "softphone");
			XmlNode gateways = XmlUtils.AddNodeNode(profile, "gateways");
			Account.create_gateway_nodes(gateways, FieldValue.GetByName(values, "tls").value == "true", FieldValue.GetByName(values, "tls-only").value == "true");
			XmlNode settings = XmlUtils.AddNodeNode(profile, "settings");

			Utils.add_xml_param(settings, "context", "public");
			Utils.add_xml_param(settings, "dialplan", "xml");
			Utils.add_xml_param(settings, "disable-register", "true");
			bool tls_cert_check_already = false;
			foreach (FieldValue value in values) {
				if (String.IsNullOrEmpty(value.field.xml_name))
					continue;
				if (String.IsNullOrWhiteSpace(value.value) && !AllowedEmptyFields.Contains(value.field.name))
					continue;
				String param_value = value.value;
				if ((value.field.name=="tls-only" || value.field.name == "tls") && value.value == "true" && !tls_cert_check_already) {
					if (!tls_cert_exist_check())
						param_value = "false";
					else
						tls_cert_check_already = true;
				}

                if(     value.field.name == "ext-rtp-ip" 
                    ||  value.field.name == "ext-sip-ip" 
                    ||  value.field.name == "rtp-ip"
                    ||  value.field.name == "sip-ip")
                {                   
                    if (value.value.EndsWith(".x"))
                    {
                        // finding matching dynamic local IP
                        string ipPart = value.value.Substring(0, value.value.IndexOf(".x")+1);
                        foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
                        {
                            if (item.OperationalStatus == OperationalStatus.Up)
                            {
                                foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                                {
                                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                                    {
                                        if (ip.Address.ToString().StartsWith(ipPart))
                                        {
                                            param_value = ip.Address.ToString();
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

				Utils.add_xml_param(settings, value.field.xml_name, param_value);
				if (value.field.xml_name == "codec-prefs") {
					Utils.add_xml_param(settings, "inbound-codec-prefs", param_value);
					Utils.add_xml_param(settings, "outbound-codec-prefs", param_value);
				}
			}

			DelayedFunction.DelayedCall("SofiaProfileCheck", sofia_profile_check, 1000);
		}
		private bool tls_cert_exist_check(){
			String base_dir = FieldValue.GetByName(values, "tls-cert-dir").value;
			if (String.IsNullOrWhiteSpace(base_dir))
				base_dir = "conf/ssl";
			//this is what freeswitch uses by default if its empty, if this changes this code needs to be updated
			base_dir = base_dir.Replace('/', '\\'); //windows file path
			if (base_dir[base_dir.Length - 1] != '\\')
				base_dir += '\\';
			if (!System.IO.File.Exists(base_dir + "cafile.pem")) {
				MessageBox.Show("Your sofia settings have TLS enabled however you do not have a cafile.pem in your cert folder, this will most likely cause the entire softphone profile not to load so I am disabling TLS in the profile for now");
				return false;
			}
			return true;
		}
		public enum RELOAD_CONFIG_MODE {
			SOFT,
			HARD,
			MODULE
		} ;
		public void reload_config(RELOAD_CONFIG_MODE mode) {
			switch (mode) {
				case RELOAD_CONFIG_MODE.SOFT:
					Utils.api_exec("sofia", "profile softphone rescan reloadxml");
					break;
				case RELOAD_CONFIG_MODE.HARD:
					Utils.api_exec("sofia", "profile softphone restart reloadxml");
					break;
				case RELOAD_CONFIG_MODE.MODULE:
					Utils.api_exec("reload", "mod_sofia");
					DelayedFunction.DelayedCall("SofiaProfileCheck", sofia_profile_check, 1500);
					break;
			}
		}

		private bool _master_profile_ok;
		private bool master_profile_ok{
			get { return _master_profile_ok; }
			set{
				_master_profile_ok = value;
				if (!value)
				{
					foreach (var acct in Account.accounts)
					{
						if (acct.enabled)
							acct.state = "SOFIA OFFLINE";
					}
				}
			}
		}
		private bool sofia_actual_profile_check(bool is_last_try){
			String res = Utils.api_exec("sofia", "xmlstatus profile softphone").ToLower().Trim();
			if (res == "invalid command!"){
				if (! is_last_try)
					return false;
				MessageBox.Show("Warning mod_sofia module does not seem to be loaded please make sure it exists");
				master_profile_ok = false;
				return false;
			}
			if (res == "invalid profile!") {
				if (! is_last_try)
					return false;
				String tls_port_msg = "";
				if (FieldValue.GetByName(values, "tls").value == "true")
					tls_port_msg += " and tls bind port (" + FieldValue.GetByName(values, "tls-sip-port").value + ")";
				var message_res = MessageBox.Show("Warning the master sofia profile was not able to load and the phone will most likely _not_ work, make sure the local bind port (" + FieldValue.GetByName(values, "sip-port").value + ")" + tls_port_msg + " is free(set under the Advanced tab of in the sofia settings) and FSClient is allowed through your firewall, otherwise check the freeswitch.log for more details. This can sometimes happen when you lose network connection. You can try reloading the sofia profile by editing the sofia settings and clicking save to see if fixed.   Do you want to try and reload sofia now?","Sofia Profile Not Loaded",MessageBoxButton.YesNo);
				master_profile_ok = false;
				if (message_res == MessageBoxResult.Yes)
					reload_config(RELOAD_CONFIG_MODE.MODULE);
				return false;
			}
			if (res.Contains("<context>public</context>") == false){
				if (!is_last_try)
					return false;
				master_profile_ok = false;
				MessageBox.Show("I believe there may be a problem with sofia, but I do not know what");
				return false;
			}
			master_profile_ok = true;
			return true;
		}
		public void sofia_profile_check_last(){
			
			bool res = sofia_actual_profile_check(true);
		}

		public void sofia_profile_check() {
			if (!sofia_actual_profile_check(false))
				DelayedFunction.DelayedCall("SofiaProfileCheck", sofia_profile_check_last, 1200);
		}
		public void edit() {
			if (Broker.get_instance().active_calls != 0) {
				MessageBoxResult mres = MessageBox.Show("Warning editing sofia settings will cause sofia to restart and will drop any active calls, do you want to continue?", "Restart Warning", MessageBoxButton.YesNo);
				if (mres != MessageBoxResult.Yes)
					return;
			}
			GenericEditor editor = new GenericEditor();
			editor.Init("Editing Sofia Settings", values);
			editor.ShowDialog();
			if (editor.DialogResult == true) {
				if (master_profile_ok)
					reload_config(RELOAD_CONFIG_MODE.HARD);
				else
					reload_config(RELOAD_CONFIG_MODE.MODULE);
			}
		}
	}
}

