﻿//
//  EntryPanel.cs
//
//  Author(s):
//      David Lechner <david@lechnology.com>
//
//  Copyright (C) 2012-2014  David Lechner
//
//  This program is free software; you can redistribute it and/or
//  modify it under the terms of the GNU General Public License
//  as published by the Free Software Foundation; either version 2
//  of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, see <http://www.gnu.org/licenses>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using KeePass.Forms;
using System.IO;
using dlech.SshAgentLib;
using KeePass.Util;

namespace KeeAgent.UI
{
  public partial class EntryPanel : UserControl
  {
    PwEntryForm pwEntryForm;

    public EntrySettings IntialSettings {
      get;
      private set;
    }

    public EntrySettings CurrentSettings {
      get;
      private set;
    }

    public EntryPanel()
    {
      InitializeComponent();

      // make transparent so tab styling shows
      SetStyle(ControlStyles.SupportsTransparentBackColor, true);
      BackColor = Color.Transparent;
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      pwEntryForm = ParentForm as PwEntryForm;
      if (pwEntryForm != null) {
        IntialSettings =
          pwEntryForm.EntryRef.GetKeeAgentSettings();
        CurrentSettings = (EntrySettings)IntialSettings.Clone ();
        entrySettingsBindingSource.DataSource = CurrentSettings;
        keyLocationPanel.KeyLocationChanged += delegate {
          UpdateKeyInfoDelayed();
        };
        pwEntryForm.FormClosing += delegate {
          while (delayedUpdateKeyInfoTimer.Enabled)
            Application.DoEvents();
        };
      } else {
        Debug.Fail("Don't have settings to bind to");
      }
      UpdateControlStates();
    }

    private void UpdateControlStates()
    {
      addKeyAtOpenCheckBox.Enabled = hasSshKeyCheckBox.Checked;
      removeKeyAtCloseCheckBox.Enabled = hasSshKeyCheckBox.Checked;
      confirmConstraintCheckBox.Enabled = addKeyAtOpenCheckBox.Enabled;
      lifetimeConstraintCheckBox.Enabled = addKeyAtOpenCheckBox.Enabled;
      lifetimeConstraintNumericUpDown.Enabled = lifetimeConstraintCheckBox.Enabled
        && lifetimeConstraintCheckBox.Checked;
      keyLocationPanel.Enabled = hasSshKeyCheckBox.Checked;
      UpdateKeyInfoDelayed();
    }

    void UpdateKeyInfoDelayed()
    {
      // Have to delay execution of this to avoid undesirable binding
      // interaction.
      if (!delayedUpdateKeyInfoTimer.Enabled)
        delayedUpdateKeyInfoTimer.Start();
    }

    void UpdateKeyInfo()
    {
      if (hasSshKeyCheckBox.Checked) {
        switch (keyLocationPanel.KeyLocation.SelectedType) {
          case EntrySettings.LocationType.Attachment:
          case EntrySettings.LocationType.File:
            try {
              pwEntryForm.UpdateEntryStrings(true, false);
              using (var key = CurrentSettings.
                GetSshKey(pwEntryForm.EntryStrings, pwEntryForm.EntryBinaries)) {
                commentTextBox.Text = key.Comment;
                fingerprintTextBox.Text = key.GetMD5Fingerprint().ToHexString();
                publicKeyTextBox.Text = key.GetAuthorizedKeyString();
                copyPublicKeybutton.Enabled = true;
              }
            } catch (Exception) {
              string file = "attachment";
              if (keyLocationPanel.KeyLocation.SelectedType == EntrySettings.LocationType.File) {
                try {
                  file = Path.GetFullPath(CurrentSettings.Location.FileName);
                } catch (Exception) {
                  file = "file";
                }
              }
              commentTextBox.Text = string.Format("<Error loading key from {0}>",
                  file);
              fingerprintTextBox.Text = string.Empty;
              publicKeyTextBox.Text = string.Empty;
              copyPublicKeybutton.Enabled = false;
            }
            break;
          default:
            commentTextBox.Text = "No key selected";
            fingerprintTextBox.Text = string.Empty;
            publicKeyTextBox.Text = string.Empty;
            copyPublicKeybutton.Enabled = false;
            break;
        }
      } else {
        commentTextBox.Text = string.Empty;
        fingerprintTextBox.Text = string.Empty;
        publicKeyTextBox.Text = string.Empty;
      }
    }

    private void hasSshKeyCheckBox_CheckedChanged(object sender, EventArgs e)
    {
      UpdateControlStates();
    }

    private void lifetimeConstraintCheckBox_CheckedChanged(object sender, EventArgs e)
    {
      UpdateControlStates();
    }

    private void helpButton_Click(object sender, EventArgs e)
    {
      Process.Start(Properties.Resources.WebHelpEntryOptions);
    }

    private void copyPublicKeybutton_Click(object sender, EventArgs e)
    {
      ClipboardUtil.Copy(publicKeyTextBox.Text, false, false, null, null, pwEntryForm.Handle);
    }

    private void delayedUpdateKeyIndoTimer_Tick(object sender, EventArgs e)
    {
      delayedUpdateKeyInfoTimer.Stop();
      UpdateKeyInfo();
    }
  }
}
