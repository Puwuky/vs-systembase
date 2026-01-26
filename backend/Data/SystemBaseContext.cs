using System;
using System.Collections.Generic;
using Backend.Models.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public partial class SystemBaseContext : DbContext
{
    public SystemBaseContext()
    {
    }

    public SystemBaseContext(DbContextOptions<SystemBaseContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Menus> Menus { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<Usuarios> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost,1433;Database=systemBase;User Id=sa;Password=Password123!;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Menus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Menus__3214EC07DD4A0977");

            entity.HasIndex(e => e.Activo, "IX_Menus_Activo");

            entity.HasIndex(e => e.Orden, "IX_Menus_Orden");

            entity.HasIndex(e => e.PadreId, "IX_Menus_PadreId");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Icono)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Ruta)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Titulo)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Padre).WithMany(p => p.InversePadre)
                .HasForeignKey(d => d.PadreId)
                .HasConstraintName("FK_Menus_Padre");
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC0772092302");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasMany(d => d.Menu).WithMany(p => p.Rol)
                .UsingEntity<Dictionary<string, object>>(
                    "RolMenu",
                    r => r.HasOne<Menus>().WithMany()
                        .HasForeignKey("MenuId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__RolMenu__MenuId__5070F446"),
                    l => l.HasOne<Roles>().WithMany()
                        .HasForeignKey("RolId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__RolMenu__RolId__4F7CD00D"),
                    j =>
                    {
                        j.HasKey("RolId", "MenuId").HasName("PK__RolMenu__E5BAEFD240AA35FD");
                    });
        });

        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuarios__3214EC0708790CE8");

            entity.HasIndex(e => e.Username, "UQ__Usuarios__536C85E4130E8900").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Usuarios__A9D10534197F2EFB").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Apellido)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .HasConstraintName("FK_Usuarios_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
