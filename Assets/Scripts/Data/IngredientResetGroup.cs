using UnityEngine;

public class IngredientResetGroup : MonoBehaviour
{
    private Ingredient[] ingredients;
    private Vector3[] startPositions;
    private Quaternion[] startRotations;
    private int[] startLayers;
    private Rigidbody2D[] bodies;
    private float[] startGravities;
    private Collider2D[] colliders;

    private void Awake()
    {
        // Find all Ingredient components underneath this parent (even inactive children)
        ingredients = GetComponentsInChildren<Ingredient>(true);

        int n = ingredients.Length;
        startPositions = new Vector3[n];
        startRotations = new Quaternion[n];
        startLayers    = new int[n];
        bodies         = new Rigidbody2D[n];
        startGravities = new float[n];
        colliders      = new Collider2D[n];

        for (int i = 0; i < n; i++)
        {
            var ing = ingredients[i];
            if (ing == null) continue;

            var t  = ing.transform;
            var go = ing.gameObject;

            // Remember starting transform + layer
            startPositions[i] = t.position;
            startRotations[i] = t.rotation;
            startLayers[i]    = go.layer;

            // Remember physics state
            var rb = ing.GetComponent<Rigidbody2D>();
            bodies[i] = rb;
            if (rb != null)
                startGravities[i] = rb.gravityScale;

            // Remember collider
            colliders[i] = ing.GetComponent<Collider2D>();
        }
    }

    [ContextMenu("Reset Ingredients")]
    public void ResetIngredients()
    {
        if (ingredients == null) return;

        for (int i = 0; i < ingredients.Length; i++)
        {
            var ing = ingredients[i];
            if (ing == null) continue;

            var t  = ing.transform;
            var go = ing.gameObject;

            // Reactivate object
            go.SetActive(true);

            // Restore transform
            t.position = startPositions[i];
            t.rotation = startRotations[i];

            // Restore layer (important for raycast click)
            go.layer = startLayers[i];

            // Reset consumed state so beaker can use it again
            ing.consumed = false;

            // Reset physics
            var rb = bodies[i];
            if (rb != null)
            {
                rb.velocity        = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale    = startGravities[i];
                rb.Sleep();
                rb.WakeUp();
            }

            // Ensure collider is enabled
            var col = colliders[i];
            if (col != null)
                col.enabled = true;
        }
    }
}
