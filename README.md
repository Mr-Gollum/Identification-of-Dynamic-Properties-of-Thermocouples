# Measurement-system-for-thermoelectric-modules

![Completedstructure1080](https://github.com/user-attachments/assets/3b2427d6-4160-4dba-9bcb-e8ef23ab64e5)

The work aims to identify the characteristics of thermocouples such as SP1848-27145 to 
obtain a functional measurement system. This system is controlled by ESP32 and wirelessly 
operated by a computer. The microcontroller control application is developed in C# .NET 
language, with communication secured via Bluetooth serial. The created system records 
essential information such as the voltage generated by the thermocouples under various 
temperature conditions. Voltage is measured using the ADS1115 converter, temperatures 
using DS18B20 sensors, and current and power are both computed. 
The application displays all information, from which graphs and raw data can be exported for further 
processing.

## Specs & Features
- Voltage (1x gain) ±4.096V(±0.15-0.01%),  1 bit = 2mV,  1 step 0.125mV - gain can be changed in code
- Heat Generation 4x50W
- Recommended maximum Temperature 100°C, Maximum 125°C - limited by components
- The difference in desired and measured temperature is -0,3°C +0,2°C when set to 50°C
- Can be controlled by application or every device with Bluetooth serial communication
- Full duplex Bluetooth communication
- 2 automatic operating mods
- Full metal body with minimal heat transfer from heated plate to body
- Can measure Peltier modules and Thermoelectric generators
- Measured thermoelectric modules are held by high torque servo motor
- Can reset measured values depending on your needs

Master's thesis:
![Master Work.pdf[Slovak]](https://github.com/Mr-Gollum/Identification-of-Dynamic-Properties-of-Thermocouples/blob/main/Documents/Master%20Work%20%5BSlovak%5D.pdf)

---
# Scheme
![scheme](https://github.com/user-attachments/assets/79884803-7c06-436e-8a5a-b6c42a991a75)

---

# Comparison of voltage profiles at the desired temperature of 80°C
![image](https://github.com/user-attachments/assets/9606e6b5-952d-43d8-81a8-d56d8d05d185)

---

# How to use

Download zip files from the release. Open Arduino code in Arduino IDE and VS code in VS. Upload code to ESP32 and pair PC with ESP32. 
Set COM ports in the C# application and connect via the connect button

# 1. Mode - hold the desired temperature

https://github.com/user-attachments/assets/8382e668-b9b2-4c0a-bfda-ecc3a6985fa0

Set desired and maximal temperature, resistor value, COM ports, and operating mode in settings. 
After measurement, you can export data using the export button. The app will ask you if you want to keep data in charts. If you want to keep it click yes (Data export + keep data). If you do not need data in charts select "no" (Data export + clear data). If you want to break the operation (export) then click "cancel" (No data export + keep data). 
Data will look like in [a file](https://github.com/RealMrGollum/Identification-of-Dynamic-Properties-of-Thermocouples/tree/main/Measurement%20of%20SP1848)

# 2. Mode - Temperature difference

https://github.com/user-attachments/assets/7c61cacc-1df6-4d22-9f17-7aeec5a60458


