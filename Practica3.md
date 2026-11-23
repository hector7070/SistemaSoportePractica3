# Práctica Programada 3: Etiquetas en el MVC
**Curso:** SC-701 Programación Avanzada Web (.NET)  
**Estudiante:** Héctor Méndez  
**Rama de trabajo:** `practica3`

---

## 4. Parte B: Investigación

### 1. ¿Qué es el patrón Post-Redirect-Get y qué problema concreto evita? Explíquelo con lo que pasaría si lo omite y el usuario presiona F5.

El patrón **Post-Redirect-Get (PRG)** sirve para separar el procesamiento de un formulario de la respuesta que ve el usuario en pantalla. Cuando el navegador envía datos mediante un `POST` y todo sale bien, en lugar de devolver la vista HTML de una vez (`return View()`), el controlador responde con una redirección (HTTP 302/303) hacia una acción de consulta `GET` (por ejemplo, redirige a `Detalle` o a `Index`).

El problema concreto que evita es el **doble envío de formularios** por accidente. Si omitiéramos este patrón y devolviéramos la vista directamente dentro del `POST`, la última petición que queda registrada en el historial del navegador sigue siendo ese `POST`. Si el usuario presiona **F5** para recargar la página, el navegador le mostrará el aviso de reenvío de formulario y volverá a ejecutar exactamente el mismo `POST`. Eso haría que el sistema intente guardar la etiqueta otra vez en la base de datos, generando registros duplicados o provocando un error `409 Conflict` porque el nombre ya existe.

*Fuente:* [Microsoft Learn - Handling Form Submissions and Post-Redirect-Get in ASP.NET Core](https://learn.microsoft.com/aspnet/core/mvc/controllers/actions)

---

### 2. El cliente tipado se registra con `AddHttpClient<TInterfaz, TImplementacion>`. ¿Qué tiempo de vida (Transient, Scoped o Singleton) tiene y por qué no conviene que sea Singleton ni crear `new HttpClient()` a mano?

Cuando registramos un cliente tipado con `AddHttpClient<IEtiquetasApi, EtiquetasApi>`, la clase del cliente se registra en el contenedor de inyección de dependencias con tiempo de vida **Transient** (se crea una instancia nueva cada vez que se inyecta en un controlador). Sin embargo, por debajo `IHttpClientFactory` administra un *pool* de `HttpMessageHandler` y los va reciclando automáticamente cada cierto tiempo (por defecto cada 2 minutos).

* **Por qué no conviene que sea Singleton:** Un Singleton mantendría las mismas conexiones abiertas durante toda la vida de la aplicación, por lo que no se enteraría si el servidor o el API cambia de dirección IP en el DNS (*DNS staleness*), quedando apuntando a una dirección vieja.
* **Por qué no conviene hacer `new HttpClient()` a mano:** Crear y destruir instancias manuales en cada llamada provoca **agotamiento de sockets** (*socket exhaustion*). Aunque llamemos a `Dispose()`, el sistema operativo deja los puertos ocupados en estado `TIME_WAIT` durante varios minutos; si entran varias peticiones seguidas, la aplicación se queda sin puertos disponibles y falla.

*Fuente:* [Microsoft Learn - Make HTTP requests using IHttpClientFactory in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/http-requests)

---

### 3. ¿Qué es ProblemDetails (RFC 9457)? Nombre los campos que su MVC lee y diga cuál es la diferencia entre el formato de un 400 de validación y el de un 409.

`ProblemDetails` es el estándar HTTP (RFC 9457, que actualizó al RFC 7807) para devolver errores en formato JSON (`application/problem+json`) de manera ordenada y predecible. Gracias a esto, el MVC siempre sabe dónde buscar la información del error sin importar qué endpoint haya fallado.

En nuestro proyecto MVC, a través de `ApiException` y el modelo `ProblemaApi`, se leen tres campos del JSON:
* **`title`**: el título o resumen corto del error.
* **`detail`**: la explicación específica de qué falló.
* **`errors`**: un diccionario (`Dictionary<string, string[]>`) con los mensajes de error separados por cada propiedad del formulario.

La diferencia entre el formato de un **400** y el de un **409** está en cómo transportan el mensaje:
* **Error 400 (Bad Request):** Cuando falla una validación del modelo, el API usa `ValidationProblemDetails` y llena el diccionario **`errors`** con el nombre exacto de la propiedad que falló (por ejemplo, `"Nombre": ["El nombre debe tener entre 2 y 40 caracteres."]`). Con esa clave, el MVC sabe exactamente debajo de cuál input pintar el mensaje en el formulario.
* **Error 409 (Conflict):** Ocurre por un conflicto en las reglas de negocio (por ejemplo, intentar crear una etiqueta con un nombre que ya existe). Este no trae el diccionario `errors`, sino que envía el mensaje dentro de **`detail`** (por ejemplo, `"Ya existe la etiqueta 'Facturación'."`). En el controlador del MVC ese texto se agrega a `ModelState` con clave vacía (`string.Empty`) para que aparezca arriba en el resumen general (`asp-validation-summary="ModelOnly"`).

*Fuente:* [IETF RFC 9457 - Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc9457.html) | [Microsoft Learn - Handle errors in ASP.NET Core web APIs](https://learn.microsoft.com/aspnet/core/web-api/handle-errors)

---

### 4. ¿Qué ataque previene `[ValidateAntiForgeryToken]`? Explique cómo lo hace ASP.NET Core y qué se pone en el formulario.

El atributo `[ValidateAntiForgeryToken]` previene ataques **CSRF / XSRF** (*Cross-Site Request Forgery* o falsificación de petición en sitios cruzados). Este ataque ocurre cuando un usuario tiene su sesión abierta en nuestra aplicación y visita otra página maliciosa; esa página podría enviar un formulario oculto hacia nuestro sistema aprovechando que el navegador manda automáticamente las cookies de sesión.

ASP.NET Core lo evita con una validación de doble token:
1. Al cargar el formulario, el servidor genera un valor aleatorio seguro y lo envía en dos lugares distintos: dentro de una cookie interna (`.AspNetCore.Antiforgery`) y dentro del HTML del formulario.
2. Cuando el usuario envía el `POST`, el filtro `[ValidateAntiForgeryToken]` revisa que el token que viene en el formulario coincida con el de la cookie. Como una página externa no puede leer nuestras cookies debido a la política de mismo origen del navegador (*Same-Origin Policy*), no tiene forma de generar ese valor y el servidor rechaza la petición con un error `400`.

En el formulario HTML se agrega un campo oculto con esta estructura:
```html
<input name="__RequestVerificationToken" type="hidden" value="CfDJ8..." />
```
En ASP.NET Core MVC, al utilizar el Tag Helper de Razor en una etiqueta `<form method="post">`, este campo se genera e inyecta de forma automática.

*Fuente:* [Microsoft Learn - Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery)

---

### 4.2. Compare `ViewData`, `ViewBag`, `TempData` y un modelo tipado: ¿cuándo usaría cada uno? Dé un ejemplo de su propia práctica.

- **Modelo tipado (`@model`):**  
 Cuándo usarlo: Es la opción principal cuando vamos a enviar o recibir los datos propios de una pantalla o formulario. Nos da seguridad de tipos en compilación, autocompletado en Visual Studio y validación automática con Data Annotations.

Ejemplo en la práctica: En EtiquetasController, pasamos List<EtiquetaVm> a la vista Index para armar la tabla de etiquetas, y recibimos EtiquetaCrearVm en Crear para validar los campos Nombre y Color.

- **`ViewData`:**  
Cuándo usarlo: Es un diccionario (ViewDataDictionary) que vive únicamente durante la petición HTTP actual. Sirve para pasar datos pequeños o de configuración visual desde el controlador hacia el _Layout sin tener que meterlos dentro del modelo principal.

Ejemplo en la práctica: En las vistas Index.cshtml, Crear.cshtml y Detalle.cshtml usamos ViewData["Title"] = "Etiquetas" para configurar el título de la pestaña del navegador en _Layout.cshtml.

- **`ViewBag`:**  
Cuándo usarlo: Funciona igual que ViewData y dura solo una petición, pero usa propiedades dinámicas (ViewBag.Title) en lugar de llaves de diccionario. Es cómodo para pasar un dato rápido sin declarar llaves, aunque si escribimos mal el nombre de una propiedad el error no salta hasta que se ejecuta la vista.

- **`TempData`:**  
  Cuándo usarlo: Es un diccionario respaldado por cookies o sesión que permite guardar un dato para que sobreviva a una redirección (RedirectToAction). Apenas se lee en la siguiente pantalla, se elimina automáticamente. Es fundamental para mostrar notificaciones cuando usamos el patrón Post-Redirect-Get.

Ejemplo en la práctica: En EtiquetasController.cs, después de crear la etiqueta en el método Crear (POST), guardamos TempData["Exito"] = $"Etiqueta '{creada.Nombre}' creada con éxito." antes de redirigir a Detalle, permitiendo que esa vista muestre la alerta de confirmación tras la redirección.

*Fuente:* [Microsoft Learn - Passing data to views in ASP.NET Core MVC](https://learn.microsoft.com/aspnet/core/mvc/views/overview#passing-data-to-views)

---

## 5. Evidencias de Funcionamiento

Las capturas de pantalla generadas se encuentran almacenadas dentro del directorio `docs/evidencia/`:

1. Lista de etiquetas con vista parcial funcionando:

Muestra la tabla de etiquetas registradas en el sistema, donde la columna Insignia utiliza la vista parcial _InsigniaColor para renderizar el color hexadecimal de cada etiqueta.
   ![Lista de Etiquetas](docs/evidencia/1-lista-etiquetas.png)

2. Formulario con error de validación bajo un campo:

Muestra el formulario de creación de etiqueta cuando se ingresa un valor inválido (por ejemplo, un nombre de 1 carácter), desplegando el mensaje de validación debajo del campo correspondiente.
   ![Error de Validación](docs/evidencia/2-formulario-error-validacion.png)

3. Formulario con mensaje de nombre repetido (error 409):

Muestra el formulario tras intentar registrar un nombre de etiqueta que ya existe. El API responde con HTTP 409 (Conflicto) y el MVC muestra el mensaje dentro del resumen de validación superior.
   ![Error 409 Nombre Repetido](docs/evidencia/3-formulario-error-409.png)

---

## 8. Declaración de Uso de Herramientas de Apoyo

Para el desarrollo de esta práctica programada se utilizaron las siguientes herramientas y recursos de apoyo:
* **Documentación oficial de Microsoft Learn:** Consulta sobre clientes tipados con `IHttpClientFactory`, manejo de errores con `ProblemDetails` (RFC 9457), protección CSRF y arquitectura MVC en .NET 10.
* **Herramientas de línea de comandos de .NET (`dotnet CLI`):** Compilación, configuración de User Secrets (`dotnet user-secrets`), migraciones y ejecución del proyecto.
* **Scalar / OpenAPI UI:** Verificación interactiva de los endpoints, esquemas JSON y códigos de respuesta HTTP en `http://localhost:5190/scalar/v1`.
* **Apuntes y ejemplos de clase (Semanas 9, 10 y 11):** Guía para la arquitectura en capas, el patrón Post-Redirect-Get y el manejo de `ApiException`.
