using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class Usuarios
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int? RolId { get; set; }

    public virtual Roles? Rol { get; set; }
}
