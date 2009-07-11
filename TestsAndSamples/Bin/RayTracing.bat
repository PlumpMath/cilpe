@echo off
echo %TIME%
RayTracing.exe 1 1000
echo %TIME%
cd CILPEOutPut
echo %TIME%
RayTracing.exe 1 1000
echo %TIME%
