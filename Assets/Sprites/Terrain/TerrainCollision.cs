using UnityEngine;


[RequireComponent(typeof(TerrainContext))]
public class TerrainCollision : MonoBehaviour
{
    private TerrainContext _terrainContext;


    private void Awake()
    {
        _terrainContext = GetComponent<TerrainContext>();
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.rigidbody;

        float mass;
        if (rb == null)
        {
            mass = _terrainContext.Rigidbody.mass;
        }
        else
        {
            mass = rb.mass;
        }

        float speed = collision.relativeVelocity.magnitude;
        float impact = mass * speed / _terrainContext.Rigidbody.mass;

        if (impact < 3.0f)
            return;

        CrackParameter crack;
        crack.direction = collision.relativeVelocity.normalized;
        crack.angleNoise = 90.0f;
        crack.minCrackCount = 1;
        crack.maxCrackCount = 2;

        _terrainContext.Destruct(collision.contacts[0].point, 0.01f * _terrainContext.Rigidbody.mass, crack);

        //Rigidbody2D thisRigid = _terrainContext.Rigidbody;
        //Rigidbody2D otherRigid = collision.rigidbody;

        //// Š·ŽZŽ¿—Ê‚ð‹‚ß‚é
        //float reducedMass = GetReducedMass(collision);

        //// ‰^“®ƒGƒlƒ‹ƒM[‚ð‹‚ß‚é
        //float sqrSpeed = collision.relativeVelocity.sqrMagnitude;
        //float energy = 0.5f * reducedMass * sqrSpeed;

        //// ˆê’èˆÈ‰º‚ÌÕŒ‚‚È‚çˆ—‚ðƒXƒLƒbƒv
        //if (Mathf.Sqrt(energy) < _terrainContext.TerrainSettings.MinImpulse)
        //    return;

        //CrackParameter crack;
        //crack.direction = collision.relativeVelocity.normalized;
        //crack.angleNoise = 90.0f;
        //crack.minCrackCount = 1;
        //crack.maxCrackCount = 2;
        //_terrainContext.Destruct(collision.contacts[0].point,
        //    Mathf.Sqrt(energy) * _terrainContext.TerrainSettings.ImpulseToRadius, crack);
    }


    // Š·ŽZŽ¿—Ê‚ð‹‚ß‚é
    private float GetReducedMass(Collision2D collision)
    {
        Rigidbody2D thisRigid = _terrainContext.Rigidbody;
        Rigidbody2D otherRigid = collision.rigidbody;

        // Š·ŽZŽ¿—Ê‚ð‹‚ß‚é
        float reducedMass = 0.0f;
        if (thisRigid != null && otherRigid != null)
        {
            float thisMass = thisRigid.mass;
            float otherMass = otherRigid.mass;
            reducedMass = (thisMass * otherMass) / (thisMass + otherMass);
        }
        else if (thisRigid != null)
        {
            reducedMass = thisRigid.mass;
        }
        else if (otherRigid != null)
        {
            reducedMass = otherRigid.mass;
        }

        return reducedMass;
    }
}
