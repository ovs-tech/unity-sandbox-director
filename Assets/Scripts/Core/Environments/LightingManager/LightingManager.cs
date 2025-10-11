using UnityEngine;

namespace Core.Environment.LightingManager
{
    [ExecuteAlways]
    public class LightingManager : MonoBehaviour
    {
        //Scene References
        [SerializeField] private Light DirectionalLight;
        [SerializeField] private LightingPreset Preset;
        //Variables
        [SerializeField, Range(0, 24)] private float TimeOfDay;
        [SerializeField] private bool EnableRuntime = true;
        [SerializeField] private float TimeScale = 24f; // 24 = 1 in-game minute = 60 real seconds (24 hours = 24 hours real time)


        private void Update()
        {
            if (Preset == null)
                return;

            if (Application.isPlaying && EnableRuntime)
            {
                //(Replace with a reference to the game time)
                // TimeScale: 24 = 1 in-game minute per 60 real seconds (full day cycle = 24 hours real time)
                TimeOfDay += Time.deltaTime * (TimeScale / 86400f); // Convert to hours per second (86400 = seconds in a day)
                TimeOfDay %= 24; //Modulus to ensure always between 0-24
                UpdateLighting(TimeOfDay / 24f);
            }
            else
            {
                UpdateLighting(TimeOfDay / 24f);
            }
        }


        private void UpdateLighting(float timePercent)
        {
            //Set ambient and fog
            RenderSettings.ambientLight = Preset.AmbientColor.Evaluate(timePercent);
            RenderSettings.fogColor = Preset.FogColor.Evaluate(timePercent);

            //If the directional light is set then rotate and set it's color, I actually rarely use the rotation because it casts tall shadows unless you clamp the value
            if (DirectionalLight != null)
            {
                DirectionalLight.color = Preset.DirectionalColor.Evaluate(timePercent);

                DirectionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, 170f, 0));
            }

        }

        //Try to find a directional light to use if we haven't set one
        [System.Obsolete]
        private void OnValidate()
        {
            if (DirectionalLight != null)
                return;

            //Search for lighting tab sun
            if (RenderSettings.sun != null)
            {
                DirectionalLight = RenderSettings.sun;
            }
            //Search scene for light that fits criteria (directional)
            else
            {
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        DirectionalLight = light;
                        return;
                    }
                }
            }
        }
    }
}