using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleBoxing;

public class SlowMo : Singleton<SlowMo>
{
    public float slowDownFactor = 0.05f;
    public float slowDownLength = 2f;
    bool m_isDoingSlowmo = false;


    void Update()
    {
        if (!m_isDoingSlowmo)
        {
            Time.timeScale += (1f / slowDownLength) * Time.unscaledDeltaTime;
            Time.fixedDeltaTime += (0.01f / slowDownLength) * Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
            Time.fixedDeltaTime = Mathf.Clamp(Time.fixedDeltaTime, 0f, 0.01f);
        }
    }

    /// <summary>
    /// set the slow motion for entire game simulation
    /// </summary>
    /// <param name="m_customSlowDownFactor">the default value was 0.05</param>
    public void DoSlowMotion(float m_customSlowDownFactor)
    {
        Time.timeScale = m_customSlowDownFactor;
        Time.fixedDeltaTime = Time.timeScale * .02f;
        m_isDoingSlowmo = true;
    }
    public void UndoSlowMotion()
    {
        Time.timeScale = 1f;
        m_isDoingSlowmo = false;
    }
}