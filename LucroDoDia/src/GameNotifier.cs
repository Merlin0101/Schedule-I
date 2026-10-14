using System;
using System.Linq;
using System.Reflection;
using MelonLoader;

namespace LucroDoDia
{
    /// <summary>
    /// Mostra uma notificacao nativa do jogo (canto da tela) usando o
    /// NotificationsManager por reflection, nas builds Mono e IL2CPP.
    /// Se o jogo mudar e nada for encontrado, so registra no console.
    /// </summary>
    internal static class GameNotifier
    {
        private static readonly string[] TypeNames =
        {
            "ScheduleOne.UI.NotificationsManager",
            "Il2CppScheduleOne.UI.NotificationsManager",
        };

        private static bool _resolved;
        private static PropertyInfo _instance;
        private static MethodInfo _send;

        public static void Send(string title, string subtitle)
        {
            try
            {
                Resolve();
                var manager = _instance?.GetValue(null);
                if (manager == null || _send == null)
                {
                    MelonLogger.Msg($"[LucroDoDia] {title}: {subtitle}");
                    return;
                }

                // SendNotification(titulo, subtitulo, icone, duracao, som) - preenche o resto pelo tipo.
                var parameters = _send.GetParameters();
                var args = new object[parameters.Length];
                args[0] = title;
                args[1] = subtitle;
                for (var i = 2; i < parameters.Length; i++)
                {
                    var type = parameters[i].ParameterType;
                    if (type == typeof(float)) args[i] = 5f;
                    else if (type == typeof(bool)) args[i] = true;
                    else if (parameters[i].HasDefaultValue) args[i] = parameters[i].DefaultValue;
                    else args[i] = type.IsValueType ? Activator.CreateInstance(type) : null;
                }
                _send.Invoke(manager, args);
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"[LucroDoDia] Falha ao notificar ({e.GetType().Name}). {title}: {subtitle}");
            }
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => TypeNames.Select(n => SafeGetType(a, n)).FirstOrDefault(t => t != null))
                .FirstOrDefault(t => t != null);
            if (type == null) return;

            _instance = type.GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            _send = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "SendNotification") return false;
                    var p = m.GetParameters();
                    return p.Length >= 2 && p[0].ParameterType == typeof(string) && p[1].ParameterType == typeof(string);
                });
        }

        private static Type SafeGetType(Assembly assembly, string name)
        {
            try { return assembly.GetType(name, false); }
            catch { return null; }
        }
    }
}
