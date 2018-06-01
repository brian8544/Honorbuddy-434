using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;

namespace HighVoltz.AutoAngler
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
			propertyGrid.SelectedObject = AutoAnglerSettings.Instance;
        }


        private void PropertyGridPropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (e.ChangedItem.Label == "Poolfishing")
            {
                if (!(bool) e.ChangedItem.Value)
                {
                    if (!string.IsNullOrEmpty(ProfileManager.XmlLocation))
                    {
                        AutoAnglerSettings.Instance.LastLoadedProfile = ProfileManager.XmlLocation;
                        AutoAnglerSettings.Instance.Save();
                    }
                    ProfileManager.LoadEmpty();
                }
                else if ((ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.Name == "Empty Profile") &&
                         !string.IsNullOrEmpty(AutoAnglerSettings.Instance.LastLoadedProfile) &&
                         File.Exists(AutoAnglerSettings.Instance.LastLoadedProfile))
                {
                    ProfileManager.LoadNew(AutoAnglerSettings.Instance.LastLoadedProfile);
                }
            }
			AutoAnglerSettings.Instance.Save();
        }

        private void MailButtonClick(object sender, EventArgs e)
        {
            Profile profile = ProfileManager.CurrentProfile;
            if (profile != null && profile.MailboxManager != null)
            {
                Mailbox mailbox = profile.MailboxManager.GetClosestMailbox();
                if (mailbox != null)
                {
                    if (!string.IsNullOrEmpty(CharacterSettings.Instance.MailRecipient))
                    {
                        BotPoi.Current = new BotPoi(mailbox);
						AutoAnglerBot.Log("Forced Mail run");
                        TreeRoot.StatusText = "Doing Mail Run";
                    }
                    else
						AutoAnglerBot.Log("No mail recipient set");
                }
                else
                {
					AutoAnglerBot.Log("Profile has no Mailbox");
                }
            }
        }
    }
}