using UnityEngine;

public class Background : MonoBehaviour
{
    [SerializeField] Transform cam;   // laisse vide : prend Camera.main

    [SerializeField] Transform Back5;   // Ciel
    [SerializeField] Transform Back4;   // Nuages
    [SerializeField] Transform Back3;   // Montagnes
    [SerializeField] Transform Back2;   // Collines 2
    [SerializeField] Transform Back1;   // Collines 1

    // 0 = suit la caméra (paraît infiniment loin), 1 = immobile dans le monde (paraît proche)
    [SerializeField, Range(0f, 1f)] float speed5 = 0.02f;
    [SerializeField, Range(0f, 1f)] float speed4 = 0.1f;
    [SerializeField, Range(0f, 1f)] float speed3 = 0.25f;
    [SerializeField, Range(0f, 1f)] float speed2 = 0.4f;
    [SerializeField, Range(0f, 1f)] float speed1 = 0.6f;

    [SerializeField] float cloudsAutoScroll = 0.3f;   // dérive des nuages (unités/s)

    Transform[] layers;
    float[] speeds;
    float[] widths;
    float[] startX;
    float[] startY;
    float camStartX, camStartY;
    float cloudOffset;

    void Start()
    {
        if (cam == null) cam = Camera.main.transform;

        layers = new[] { Back5, Back4, Back3, Back2, Back1 };
        speeds = new[] { speed5, speed4, speed3, speed2, speed1 };

        widths = new float[layers.Length];
        startX = new float[layers.Length];
        startY = new float[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            var sr = layers[i].GetComponent<SpriteRenderer>();
            // Largeur d'UN motif (pas celle du Tiled entier)
            widths[i] = sr.sprite.bounds.size.x * layers[i].lossyScale.x;
            startX[i] = layers[i].position.x;
            startY[i] = layers[i].position.y;
        }

        camStartX = cam.position.x;
        camStartY = cam.position.y;

        transform.position = cam.transform.position;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        cloudOffset += cloudsAutoScroll * Time.deltaTime;

        float camDX = cam.position.x - camStartX;
        float camDY = cam.position.y - camStartY;

        for (int i = 0; i < layers.Length; i++)
        {
            // Plus "speed" est petit, plus le layer suit la caméra (donc paraît lointain)
            float follow = 1f - speeds[i];

            float x = startX[i] + camDX * follow;
            float y = camDY; 

            if (i == 1) x += cloudOffset;   // Back4 = nuages

            // Rebouclage : on recale par pas d'un motif pour rester centré sur la caméra
            float shift = Mathf.Round((cam.position.x - x) / widths[i]) * widths[i];
            x += shift;

            layers[i].position = new Vector3(x, y, layers[i].position.z);
        }
    }
}