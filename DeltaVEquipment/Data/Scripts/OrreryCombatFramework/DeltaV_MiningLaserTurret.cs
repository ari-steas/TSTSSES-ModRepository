using OrreryFramework.Communication.WeaponBases;
using System;

namespace OrreryFramework.Communication
{
    partial class HeartDefinitions
    {
        WeaponDefinitionBase DeltaV_MiningLaserTurret => new WeaponDefinitionBase()
        {
            Targeting = new Targeting()
            {
                MaxTargetingRange = 1000,
                MinTargetingRange = 0,
                CanAutoShoot = true,
                RetargetTime = 0,
                AimTolerance = 0.0175f,
                DefaultIFF = IFF_Enum.TargetEnemies | IFF_Enum.TargetNeutrals,
                AllowedTargetTypes = TargetType_Enum.TargetGrids | TargetType_Enum.TargetCharacters,
            },
            Assignments = new Assignments()
            {
                BlockSubtype = "DeltaV_MiningLaserTurret",
                MuzzleSubpart = "MiningLaserTurretBarrels",
                ElevationSubpart = "MiningLaserTurretBarrels",
                AzimuthSubpart = "MiningLaserTurretBase",
                DurabilityModifier = 1,
                InventoryIconName = "",
                Muzzles = new string[]
                {
                    "muzzle_01",
                },
            },
            Hardpoint = new Hardpoint()
            {
                AzimuthRate = 0.5f,
                ElevationRate = 0.5f,
                MaxAzimuth = (float)Math.PI,
                MinAzimuth = (float)-Math.PI,
                MaxElevation = (float)Math.PI,
                MinElevation = -0.1745f,
                HomeAzimuth = 0,
                HomeElevation = 0,
                IdlePower = 10,
                ShotInaccuracy = 0.0025f,
                LineOfSightCheck = true,
                ControlRotation = true,
            },
            Loading = new Loading()
            {
                Ammos = new string[]
                {
                    DeltaVMiningLaserAmmoBeam.Name,
                },

                RateOfFire = 60,
                BarrelsPerShot = 1,
                ProjectilesPerBarrel = 1,
                ReloadTime = 1,
                DelayUntilFire = 0,
                MagazinesToLoad = 1,

                MaxReloads = -1,
            },
            Audio = new Audio()
            {
                PreShootSound = "",
                ShootSound = "",
                ReloadSound = "PunisherNewReload",
                RotationSound = "WepTurretGatlingRotate",
            },
            Visuals = new Visuals()
            {
                ShootParticle = "Muzzle_Flash_Autocannon",
                ContinuousShootParticle = false,
                ReloadParticle = "",
            },
        };
    }
}
