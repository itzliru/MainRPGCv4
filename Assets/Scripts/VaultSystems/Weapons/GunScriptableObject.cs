using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "GunScriptableObject", menuName = "ScriptableObjects/WeaponGunConfig", order = 0)]
public class GunScriptableObject : ScriptableObject
{
    [Header("Basic Info")]
    public string GunName;
    public GunType GunType;

    [Header("Configs")]
    public ShootConfigurationScriptableObject ShootConfig;
    public WeaponTrailConfig TrailConfig;
    public GameObject GetGunModel() => instantiatedModel;

    [Header("Assets")]
    public AudioClip ShootSound;
    public AudioClip ReloadSound;
    public GameObject ModelPrefab;
    public GameObject BulletPrefab;
    public GameObject MuzzleFlashPrefab;
    public GameObject ShellEjectPrefab;
    public GameObject BulletImpactPrefab;

    [Header("Offsets")]
    public Vector3 MuzzleFlashPosition;
    public Vector3 ShellEjectPosition;
    public Vector3 BulletSpawnPosition;

    // --- Private runtime state ---
    private ObjectPool<BulletPooled> bulletPool;
    private ObjectPool<GameObject> trailPool;
    private GameObject instantiatedModel;
    private Transform firePoint;

    public void InitializePools(Transform parent = null)
    {
        if (BulletPrefab == null)
        {
            Debug.LogError($"[Gun] {GunName} has no bullet prefab assigned.");
            return;
        }

        if (ShootConfig == null)
        {
            Debug.LogError($"[Gun] {GunName} has no ShootConfig assigned.");
            return;
        }

        bulletPool = new ObjectPool<BulletPooled>(
            createFunc: () =>
            {
                var obj = Instantiate(BulletPrefab, parent);
                var b = obj.GetComponent<BulletPooled>();
                if (b == null)
                {
                    Debug.LogError($"[Gun] BulletPrefab for {GunName} has no BulletPooled component.");
                    Destroy(obj);
                    return null;
                }
                return b;
            },
            actionOnGet: (b) =>
{
    if (b != null)
        b.Initialize(
            (releasedBullet) => ReleaseBullet(releasedBullet), 
            ShootConfig.BulletSpeed, 
            ShootConfig.BulletLifetime, 
            (int)ShootConfig.Damage
          
        );
},
                
            actionOnRelease: (b) =>
            {
                if (b != null)
                {
                    b.ResetBullet();
                    b.gameObject.SetActive(false);
                }
            },
            actionOnDestroy: (b) =>
            {
                if (b != null)
                    Destroy(b.gameObject);
            },
        collectionCheck: false,
        defaultCapacity: ShootConfig.MagazineSize * 2,
        maxSize: ShootConfig.MagazineSize * 5
    );

        if (TrailConfig != null && TrailConfig.TrailMaterial != null)
        {
            var trailPrefab = new GameObject($"{GunName}_Trail");
            var renderer = trailPrefab.AddComponent<TrailRenderer>();
            renderer.material = TrailConfig.TrailMaterial;
            renderer.time = TrailConfig.TrailLifetime;
            renderer.startWidth = TrailConfig.TrailWidth;
            renderer.endWidth = 0f;
            renderer.colorGradient = new Gradient()
            {
                colorKeys = new[] { new GradientColorKey(TrailConfig.TrailColor, 0), new GradientColorKey(Color.clear, 1) }
            };

            trailPool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(trailPrefab, parent),
                actionOnGet: (t) => 
                {
                    if (t != null) t.SetActive(true);
                },
                actionOnRelease: (t) => 
                {
                    if (t != null) t.SetActive(false);
                },
                actionOnDestroy: (t) => 
                {
                    if (t != null) Destroy(t);
                },
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 50
            );
        }
        else if (TrailConfig != null)
        {
            Debug.LogWarning($"[Gun] {GunName} TrailConfig has no material assigned.");
        }
    }

    public BulletPooled GetBullet()
    {
        if (bulletPool == null)
        {
            Debug.LogError($"[Gun] Bullet pool not initialized for {GunName}. Call InitializePools() first.");
            return null;
        }
        return bulletPool.Get();
    }

    public void ReleaseBullet(BulletPooled bullet)
    {
        if (bulletPool == null) return;
        if (bullet != null)
            bulletPool.Release(bullet);
    }

    public GameObject GetTrail()
    {
        if (trailPool == null) return null;
        return trailPool.Get();
    }

    public void ReleaseTrail(GameObject trail)
    {
        if (trailPool == null) return;
        if (trail != null)
            trailPool.Release(trail);
    }
/// <summary>
/// Back up method for instantiating the gun model. This is not used in the current implementation, but may be useful for future extensions where the gun model needs to be instantiated directly from the scriptable object.
/// </summary>
   // public void InstantiateModel(Transform parent = null)
   // {
     //   if (ModelPrefab == null)
     //   {
     //       Debug.LogWarning($"[Gun] {GunName} has no model prefab assigned.");
      //      return;
      //  }

      //  instantiatedModel = Instantiate(ModelPrefab, parent);
      //  instantiatedModel.name = $"{GunName}_Model";
        
      //  FindFirePoint();
       // Debug.Log($"[Gun] Instantiated model for {GunName}. FirePoint found: {(firePoint != null)}");
    //}
private Transform FindGunSocket(Transform root)
{
    if (root == null) return null;
    if (root.name == "GunSocket") return root;
    foreach (Transform child in root)
    {
        var found = FindGunSocket(child);
        if (found != null) return found;
    }
    return null;
}

public void InstantiateModel(Transform parent = null)
{
    if (ModelPrefab == null)
    {
        Debug.LogWarning($"[Gun] {GunName} has no model prefab assigned.");
        return;
    }

    Transform gunSocket = FindGunSocket(parent);
    Transform instantiateParent = gunSocket ?? parent;

    // Instantiate at instantiateParent position with its rotation
    instantiatedModel = Instantiate(ModelPrefab, instantiateParent.position, instantiateParent.rotation, instantiateParent);

    instantiatedModel.name = $"{GunName}_Model";

    // Adjust position so that RightGrip is at instantiateParent.position
    Transform rightGrip = instantiatedModel.transform.Find("RightGrip");
    if (rightGrip != null)
    {
        Vector3 offset = instantiateParent.position - rightGrip.position;
        instantiatedModel.transform.position += offset;
    }

    FindFirePoint();
}


    

    private void FindFirePoint()
    {
        if (instantiatedModel == null)
        {
            Debug.LogWarning($"[Gun] {GunName} model not instantiated.");
            return;
        }

        firePoint = instantiatedModel.transform.Find("FirePoint");
        if (firePoint == null)
        {
            Debug.LogWarning($"[Gun] {GunName} model missing 'FirePoint' child transform. Falling back to model root.");
            firePoint = instantiatedModel.transform;
        }
    }

    public Transform GetFirePoint()
    {
        if (firePoint == null && instantiatedModel != null)
        {
            FindFirePoint();
        }
        return firePoint ?? instantiatedModel?.transform;
    }

    public void CleanupModel()
    {
        if (instantiatedModel != null)
        {
            Destroy(instantiatedModel);
            instantiatedModel = null;
            firePoint = null;
        }
    }
}
