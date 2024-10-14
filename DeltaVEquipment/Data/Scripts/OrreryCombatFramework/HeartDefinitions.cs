namespace OrreryFramework.Communication
{
    partial class HeartDefinitions
    {
        internal HeartDefinitions()
        {
            LoadWeaponDefinitions(DeltaV_MiningLaserTurret, DeltaV_RailgunFixed);         //todo tell the user that they forgot to add stuff here when they get an error
            LoadAmmoDefinitions(DeltaVMiningLaserAmmoBeam, DeltaV_AmmoRailgun);
        }
    }
}
