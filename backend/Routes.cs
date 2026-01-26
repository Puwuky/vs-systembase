namespace Backend
{
    public static class Routes
    {
        public static class v1
        {
            public static class Menu
            {
                public const string Obtener = "api/v1/menu";
                public const string Tree = "api/v1/menu/tree";
                public const string Crear = "api/v1/menu";
                public const string Editar = "api/v1/menu/{id}";
                public const string Desactivar = "api/v1/menu/desactivar/{id}";
            }

            public static class Auth
            {
                public const string Login = "api/v1/auth/login";
                public const string Registrar = "api/v1/auth/registrar";
            }
        }
    }
}
