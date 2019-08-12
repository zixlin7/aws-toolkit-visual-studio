﻿using System.ComponentModel;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class KeyPairWrapper : PropertiesModel, IWrapper
    {
        AccountViewModel _account;
        string _region;
        KeyPairInfo _keyPair;

        public KeyPairWrapper(AccountViewModel account, string region, KeyPairInfo keyPair)
        {
            this._account = account;
            this._region = region;
            this._keyPair = keyPair;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Key Pair";
            componentName = this.DisplayName;
        }

        [Browsable(false)]
        public KeyPairInfo NativeKeyPair => this._keyPair;

        [Browsable(false)]
        public System.Windows.Media.ImageSource IsStoredLocallyIcon
        {
            get
            {
                string iconPath;
                if (KeyPairLocalStoreManager.Instance.DoesPrivateKeyExist(this._account, this._region, this.NativeKeyPair.KeyName))
                {
                    iconPath = "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.private-found.png";
                    var icon = IconHelper.GetIcon(this.GetType().Assembly, iconPath);
                    return icon.Source;
                }

                return null;
            }
        }

        [DisplayName("Key Pair Name")]
        public string DisplayName => NativeKeyPair.KeyName;

        [Browsable(false)]
        public string TypeName => "Key Pair";

        [DisplayName("Stored Locally")]
        public bool IsStoredLocally => KeyPairLocalStoreManager.Instance.DoesPrivateKeyExist(this._account, this._region, this.NativeKeyPair.KeyName);

        [DisplayName("Fingerprint")]
        public string Fingerprint => NativeKeyPair.KeyFingerprint;


        public void RaiseStoredLocallyEvent()
        {
            base.NotifyPropertyChanged("IsStoredLocallyIcon");
            base.NotifyPropertyChanged("IsStoredLocally");
        }
    }
}
