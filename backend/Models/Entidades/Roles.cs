using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class Roles
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();

    public virtual ICollection<Menus> Menu { get; set; } = new List<Menus>();
}
