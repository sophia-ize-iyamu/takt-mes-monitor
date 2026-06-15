# Takt shift report

OEE = Availability x Performance x Quality (Nakajima). World-class OEE is about 85%.

## OEE by work center

| Work center | Availability | Performance | Quality | OEE |
|---|---|---|---|---|
| ASSY-02 (Assembly Line) | 96% | 95% | 91% | 83% |
| CNC-01 (CNC Machining Cell) | 88% | 95% | 99% | 82% |
| TEST-03 (Final Test) | 31% | 94% | 99% | 29% |

## Downtime Pareto (minutes by reason)

| Reason | Minutes |
|---|---|
| TOOLING | 45 |
| FAULT | 30 |
| MATERIAL | 20 |
| CHANGEOVER | 15 |

## Support verdicts

| Work center | Verdict | Reason |
|---|---|---|
| ASSY-02 | HOLD | first-pass yield 91% below 95% target; OEE 83% below world-class 85% |
| CNC-01 | WATCH | availability 88% below 90% target; OEE 82% below world-class 85% |
| TEST-03 | HOLD | work order STALLED (no recent production); availability 31% below 90% target; OEE 29% below world-class 85% |

## Defect genealogy, WO-1002 (assembly excursion)

| Unit serial | Defect | Time |
|---|---|---|
| P200-0419 | SOLDER-BRIDGE | 09:14 |
| P200-0421 | MISALIGN | 09:31 |
| P200-0456 | SOLDER-BRIDGE | 10:02 |
| P200-0480 | COLD-JOINT | 10:48 |
| P200-0501 | SOLDER-BRIDGE | 11:20 |
