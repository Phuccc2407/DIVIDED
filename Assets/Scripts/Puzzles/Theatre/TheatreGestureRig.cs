using UnityEngine;

// Simple articulated mannequin for scene prototyping without an imported animation pack.
public class TheatreGestureRig : MonoBehaviour
{
    public const int GestureCount = 20;
    public Transform body, head, leftArm, rightArm, leftForearm, rightForearm, leftLeg, rightLeg;
    private Vector3 bodyOrigin;
    private bool initialized;
    public bool IsValid => body != null && head != null && leftArm != null && rightArm != null
        && leftForearm != null && rightForearm != null && leftLeg != null && rightLeg != null;

    public void Sample(int gesture, float time)
    {
        if (!IsValid) return;
        if (!initialized) { bodyOrigin = body.localPosition; initialized = true; }
        float wave = Mathf.Sin(time * Mathf.PI * 2f), pulse = 0.5f + 0.5f * wave;
        Vector3 b = Vector3.zero, h = Vector3.zero, la = Vector3.zero, ra = Vector3.zero,
            lf = Vector3.zero, rf = Vector3.zero, ll = Vector3.zero, rl = Vector3.zero;
        float height = 0f;
        switch (gesture)
        {
            case 0: ra.z = 145; rf.x = wave * 35; break; // wave right
            case 1: la.z = -145; lf.x = wave * 35; break; // wave left
            case 2: la.z = -165; ra.z = 165; break; // hands up
            case 3: la.z = -90; ra.z = 90; break; // T pose
            case 4: la.x = ra.x = -75; la.y = -35 * pulse; ra.y = 35 * pulse; break; // clap
            case 5: b.x = 25 + 20 * pulse; h.x = -10; break; // bow
            case 6: h.x = 25 * wave; break; // nod
            case 7: h.y = 45 * wave; break; // shake head
            case 8: la.z = -90; h.z = -15; break; // point left
            case 9: ra.z = 90; h.z = 15; break; // point right
            case 10: ra.z = 120; rf.z = 90; h.z = 8; break; // salute
            case 11: la.z = -35; ra.z = 35; lf.z = 95; rf.z = -95; break; // hands on hips
            case 12: la.x = ra.x = -75; lf.z = 65; rf.z = -65; break; // crossed arms
            case 13: height = 0.35f * Mathf.Abs(wave); la.z = -35; ra.z = 35; break; // hop
            case 14: ll.x = -55 * pulse; ra.x = -25; break; // left kick
            case 15: rl.x = -55 * pulse; la.x = -25; break; // right kick
            case 16: la.x = -90 * pulse; ra.x = -90 * (1 - pulse); break; // punches
            case 17: la.z = -20; ra.z = 20; b.z = 25 * wave; h.z = -b.z; break; // sway
            case 18: b.y = time * 100; la.z = -50; ra.z = 50; break; // spin
            case 19: la.z = -140; ra.z = 140; lf.x = rf.x = -55; h.x = 20; break; // cover face
        }
        body.localPosition = bodyOrigin + Vector3.up * height;
        body.localRotation = Quaternion.Euler(b); head.localRotation = Quaternion.Euler(h);
        leftArm.localRotation = Quaternion.Euler(la); rightArm.localRotation = Quaternion.Euler(ra);
        leftForearm.localRotation = Quaternion.Euler(lf); rightForearm.localRotation = Quaternion.Euler(rf);
        leftLeg.localRotation = Quaternion.Euler(ll); rightLeg.localRotation = Quaternion.Euler(rl);
    }
}
