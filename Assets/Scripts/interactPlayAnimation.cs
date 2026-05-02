using UnityEngine;

public class interactPlayAnimation : MonoBehaviour
{
    public Animator animator;
    public string AnimName;
    public AudioSource sound;


    public void PlayAnimator()
    {
        if(animator != null)
        {
            animator.SetBool(AnimName, true);
            if(sound != null )
            {
                sound.Play();
            }
        }
        else
        {
            Debug.Log("Missing animator in " + gameObject.name);
        }
    }
}
