using System;
using UnityEngine;

public class RangedAttackExecution
{
    private enum Phase
    {
        NotStarted,
        Startup,
        Recovery,
        Complete
    }

    private readonly RangedAttackData attackData;
    private readonly DamageController damageController;
    private readonly Transform attackerTransform;
    private readonly Transform projectileSpawnPoint;
    private readonly Func<Vector3> getAimDirection;

    private Phase phase = Phase.NotStarted;
    private float timer;

    public bool IsComplete => phase == Phase.Complete;

    public RangedAttackExecution(
        RangedAttackData attackData,
        DamageController damageController,
        Transform attackerTransform,
        Transform projectileSpawnPoint,
        Func<Vector3> getAimDirection)
    {
        this.attackData = attackData;
        this.damageController = damageController;
        this.attackerTransform = attackerTransform;
        this.projectileSpawnPoint = projectileSpawnPoint;
        this.getAimDirection = getAimDirection;
    }

    public void Start()
    {
        timer = 0f;
        phase = Phase.Startup;
    }

    public void Update()
    {
        if (phase == Phase.NotStarted ||
            phase == Phase.Complete)
        {
            return;
        }

        timer += Time.deltaTime;

        if (phase == Phase.Startup &&
            timer >= attackData.StartupTime)
        {
            LaunchProjectile();

            timer = 0f;
            phase = Phase.Recovery;
        }
        else if (phase == Phase.Recovery &&
                 timer >= attackData.RecoveryTime)
        {
            phase = Phase.Complete;
        }
    }

    private void LaunchProjectile()
    {
        if (attackData.ProjectilePrefab == null ||
            attackerTransform == null)
        {
            Debug.LogWarning(
                "Ranged attack is missing its projectile prefab " +
                "or attacker Transform.");
            return;
        }

        Vector3 direction = getAimDirection != null
            ? getAimDirection()
            : attackerTransform.forward;

        if (direction.sqrMagnitude <= 0.001f)
            direction = attackerTransform.forward;

        direction.Normalize();

        Vector3 spawnPosition = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : attackerTransform.position;

        Quaternion rotation =
            Quaternion.LookRotation(direction) *
            Quaternion.Euler(90f, 0f, 0f);

        GameObject projectileObject =
            UnityEngine.Object.Instantiate(
                attackData.ProjectilePrefab,
                spawnPosition,
                rotation);

        if (!projectileObject.TryGetComponent<RangedProjectile>(
                out RangedProjectile projectile))
        {
            Debug.LogError(
                $"Projectile prefab '{projectileObject.name}' " +
                "does not have a RangedProjectile component.");

            UnityEngine.Object.Destroy(projectileObject);
            return;
        }

        DamageInfo damageInfo =
            damageController.CalculateOutgoingDamage(
                attackData.DamageMultiplier,
                Vector3.zero,
                DamageSourceType.Projectile,
                attackerTransform.position);

        projectile.Initialize(
            attackerTransform,
            direction,
            attackData,
            damageInfo);
    }

    public void End()
    {
        phase = Phase.Complete;
    }
}