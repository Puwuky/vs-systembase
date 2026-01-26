using Backend.Data;
using Backend.Models.Menu;
using Backend.Models.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Gestores
{
    public static class MenuGestor
    {
        public static List<MenuItemResponse> ObtenerMenuPorUsuario(int usuarioId)
        {
            using var context = new SystemBaseContext();

            var menus = context.Menus
                .Where(m =>
                    m.Activo &&
                    m.Rol.Any(r =>
                        r.Usuarios.Any(u => u.Id == usuarioId)
                    )
                )
                .OrderBy(m => m.Orden)
                .Select(m => new MenuItemResponse
                {
                    Id = m.Id,
                    Titulo = m.Titulo,
                    Icono = m.Icono,
                    Ruta = m.Ruta,
                    Orden = m.Orden,
                    PadreId = m.PadreId
                })
                .ToList();

            return menus;
        }

        public static bool Crear(MenuRequest request)
        {
            using var context = new SystemBaseContext();

            var menu = new Menus
            {
                Titulo = request.Titulo,
                Icono = request.Icono,
                Ruta = request.Ruta,
                Orden = request.Orden,
                PadreId = request.PadreId,
                Activo = true
            };

            var roles = context.Roles
                .Where(r => request.RolesIds.Contains(r.Id))
                .ToList();

            foreach (var rol in roles)
                menu.Rol.Add(rol);

            context.Menus.Add(menu);
            context.SaveChanges();

            return true;
        }

        public static bool Editar(int id, MenuRequest request)
        {
            using var context = new SystemBaseContext();

            var menu = context.Menus
                .Include(m => m.Rol)
                .FirstOrDefault(m => m.Id == id);

            if (menu == null)
                return false;

            menu.Titulo = request.Titulo;
            menu.Icono = request.Icono;
            menu.Ruta = request.Ruta;
            menu.Orden = request.Orden;
            menu.PadreId = request.PadreId;

            menu.Rol.Clear();

            var roles = context.Roles
                .Where(r => request.RolesIds.Contains(r.Id))
                .ToList();

            foreach (var rol in roles)
                menu.Rol.Add(rol);

            context.SaveChanges();
            return true;
        }

        public static bool Desactivar(int id)
        {
            using var context = new SystemBaseContext();

            var menu = context.Menus.FirstOrDefault(m => m.Id == id);
            if (menu == null)
                return false;

            menu.Activo = false;
            context.SaveChanges();
            return true;
        }

        public static List<MenuTreeResponse> ObtenerMenuTreePorUsuario(int usuarioId)
        {
            using var context = new SystemBaseContext();

            // 1️⃣ Traemos TODOS los menús permitidos (plano)
            var menus = context.Menus
                .Where(m =>
                    m.Activo &&
                    m.Rol.Any(r =>
                        r.Usuarios.Any(u => u.Id == usuarioId)
                    )
                )
                .OrderBy(m => m.Orden)
                .Select(m => new
                {
                    m.Id,
                    m.Titulo,
                    m.Icono,
                    m.Ruta,
                    m.PadreId,
                    m.Orden
                })
                .ToList();

            // 2️⃣ Diccionario para lookup rápido
            var lookup = menus.ToDictionary(
                m => m.Id,
                m => new MenuTreeResponse
                {
                    Id = m.Id,
                    Titulo = m.Titulo,
                    Icono = m.Icono,
                    Ruta = m.Ruta,
                    Orden = m.Orden
                }
            );
            // 3️⃣ Armar el árbol
            List<MenuTreeResponse> root = new();

            foreach (var m in menus)
            {
                if (m.PadreId == null)
                {
                    root.Add(lookup[m.Id]);
                }
                else if (lookup.ContainsKey(m.PadreId.Value))
                {
                    lookup[m.PadreId.Value]
                        .Children
                        .Add(lookup[m.Id]);
                }
            }
            // Ordenar recursivamente
            void Ordenar(List<MenuTreeResponse> items)
            {
                items.Sort((a, b) => a.Orden.CompareTo(b.Orden));
                foreach (var item in items)
                {
                    Ordenar(item.Children);
                }
            }

            Ordenar(root);

            return root;
        }

    }
}
