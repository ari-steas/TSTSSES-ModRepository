using OrreryFramework.Communication.WeaponBases;
using System;

namespace OrreryFramework.Communication
{
    partial class HeartDefinitions
    {
        WeaponDefinitionBase DeltaV_MiningMetalStorm => new WeaponDefinitionBase()
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
                BlockSubtype = "DeltaV_MiningMetalStorm",
                MuzzleSubpart = "MetalStormBarrels",
                ElevationSubpart = "MetalStormBarrels",
                AzimuthSubpart = "MetalStormBase",
                DurabilityModifier = 1,
                InventoryIconName = "",
                Muzzles = new string[]
                {
                    "muzzle_storm_01",
                    "muzzle_storm_02",
                    "muzzle_storm_03",
                    "muzzle_storm_04",
                    "muzzle_storm_05",
                    "muzzle_storm_06",
                    "muzzle_storm_07",
                    "muzzle_storm_08",
                    "muzzle_storm_09",
                    "muzzle_storm_10",
                    "muzzle_storm_11",
                    "muzzle_storm_12",
                    "muzzle_storm_13",
                    "muzzle_storm_14",
                    "muzzle_storm_15",
                    "muzzle_storm_16",
                    "muzzle_storm_17",
                    "muzzle_storm_18",
                    "muzzle_storm_19",
                    "muzzle_storm_20",
                    "muzzle_storm_21",
                    "muzzle_storm_22",
                    "muzzle_storm_23",
                    "muzzle_storm_24",
                    "muzzle_storm_25",
                    "muzzle_storm_26",
                    "muzzle_storm_27",
                    "muzzle_storm_28",
                    "muzzle_storm_29",
                    "muzzle_storm_30",
                    "muzzle_storm_31",
                    "muzzle_storm_32"
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
                    DeltaVMiningMetalStormProjectile.Name,
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
                ShootSound = "metalstorm",
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
