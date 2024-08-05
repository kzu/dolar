# dolar apps

Apps para automatizar cotizaciones de USD > ARS (si, tenemos más de una `¯\_ (ツ)_/¯`).

![Dólar Libre](https://img.shields.io/endpoint?color=blue&url=https%3A%2F%2Fdolar.kzu.app%3Fbadge%26blue)
![Dólar Turista](https://img.shields.io/endpoint?color=purple&url=https%3A%2F%2Fdolar.kzu.app%3Fbadge%26tarjeta)
![Dólar MEP](https://img.shields.io/endpoint?color=green&url=https%3A%2F%2Fdolar.kzu.app%3Fbadge%26mep)
![Dólar CCL](https://img.shields.io/endpoint?color=orange&url=https%3A%2F%2Fdolar.kzu.app%3Fbadge%26ccl)
![Dólar Oficial](https://img.shields.io/endpoint?color=gold&url=https%3A%2F%2Fdolar.kzu.app%3Fbadge%26oficial)

## dotnet-dolar
<!-- #content -->
App de consola para insertar automáticamente las cotizaciones deseadas en un Excel 
que contenga una columna con la fecha de una operacion, y una columna de destino 
para el tipo de cambio de esa fecha.

<!-- include src/dotnet-dolar/help.md -->
```shell
USAGE:
    dolar <Archivo Excel> [OPTIONS]

ARGUMENTS:
    <Archivo Excel>     

OPTIONS:
                                                        DEFAULT                 
    -h, --help                                                     Prints help  
                                                                   information  
    -v, --version                                                  Prints       
                                                                   version      
                                                                   information  
    -h, --hoja                                                     Número de    
                                                                   hoja         
    -f, --fecha                                                    Número de    
                                                                   columna de   
                                                                   fecha        
    -c, --cotizacion                                               Número de    
                                                                   columna de   
                                                                   cotización   
    -t, --tipo <BILLETE|DIVISA|BLUE|MEP|CCL|TURISTA>               Tipo de      
                                                                   cotización   
                                                                   (billete y   
                                                                   divisa, del  
                                                                   BCRA         
    -o, --operacion <COMPRA|VENTA|PROMEDIO>                        Operación    
    -s                                                  True       Sobreescribir
                                                                   cotización   
                                                                   existente    
```

<!-- src/dotnet-dolar/help.md -->
