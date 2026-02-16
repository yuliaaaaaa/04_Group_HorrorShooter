using UnityEngine;

public class RaycastGun : MonoBehaviour
{
    [SerializeField] private Camera camera;
    [SerializeField] private float range = 200f;
    [SerializeField] private int damage = 25;

    private float nextTimeToFire = 0f;

    void Start()
    {
        camera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + 1f;
            Shoot();
        }
    }

    private void Shoot()
    {
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, range))
        {
            if (hit.collider.TryGetComponent<NPCHealth>(out NPCHealth npcHealth))
            {
                npcHealth.TakeDamage(damage);
            }

            Debug.DrawRay(ray.origin, hit.point, Color.red, 0.2f);
        }
    }
}