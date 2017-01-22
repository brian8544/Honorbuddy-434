#region Revision Info

// This file is part of Singular - A community driven Honorbuddy CC
// $Author: exemplar $
// $Date: 2011-05-03 18:16:12 +0300 (Sal, 03 May 2011) $
// $HeadURL: http://svn.apocdev.com/singular/trunk/Singular/Settings/MageSettings.cs $
// $LastChangedBy: exemplar $
// $LastChangedDate: 2011-05-03 18:16:12 +0300 (Sal, 03 May 2011) $
// $LastChangedRevision: 307 $
// $Revision: 307 $

#endregion


namespace Singular.Settings
{
    internal class MageSettings : Styx.Helpers.Settings
    {
        public MageSettings()
            : base(SingularSettings.SettingsPath + "_Mage.xml")
        {
        }
    }
}