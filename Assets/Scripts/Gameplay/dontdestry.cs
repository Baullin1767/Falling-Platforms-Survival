using UnityEngine;

namespace FallingPlatformsSurvival
{
    public class dontdestry : MonoBehaviour
    {
        private dontdestry d;
        void Start()
        {
            if (d == null)
            {
                d = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(this);
            }
        }
    }
}
