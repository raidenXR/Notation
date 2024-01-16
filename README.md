# Notation
simple notation lib for mathematical formulas
 
still work in progress, it lacks some fuctionality (it does not implement any hglue logic,
neither is a production ready full math notation lib. It's a very plain project, mainly done to be used
as a dependency to an other project). 

For fonts, it can use any `.ttf`, just make sure that they will contain the math symbols.
In the example, it makes use of the fonts from [KaTeX](https://github.com/KaTeX/KaTeX).

For more infomation in Typesetting read

## Bibliography
- Computers & Typesetting Vol B: TeX The Program


to build Notation.dll:  
`dotnet build Notation/Notation.csproj`


to test, run  

`dotnet build NotationExe/NotationExe.csproj`


to run .fsx  
`cd NotationExe`

`dotnet fsi test_notation.fsx`


in __notation_images__ directory it will generate some .png. They have transparent background. View in appropriate
image editor. (Window images viewer if it has dark theme will display a black screen).
Change theme to light, or open with Paint etc.


