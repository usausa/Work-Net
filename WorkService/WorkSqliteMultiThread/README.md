# Result

|WAL|Pool|Shared|Result|TPS|
|:----|:----|:----|:----|:----|
|○|○|○|OK|5255|
|○|○| |OK|5510|
|○| |○|OK|4732|
|○| | |OK|3294|
| |○|○|OK|1276|
| |○| |NG|1185|
| | |○|OK|1223|
| | | |NG|1000|

# Thread:100 WAL:true Pool:true Shared:true

```
TotalCount : 100000
TotalTime : 19029
TPS : 5255.1368963161485
Select1 : 12431
Select2 : 12497
Insert1 : 12394
Insert2 : 12660
Update1 : 12416
Update2 : 12536
Delete1 : 12393
Delete2 : 12673
Total : 100000
```

# Thread:100 WAL:true Pool:true Shared:false

```
TotalCount : 100000
TotalTime : 18145
TPS : 5510.856387082552
Select1 : 12470
Select2 : 12437
Insert1 : 12584
Insert2 : 12548
Update1 : 12623
Update2 : 12305
Delete1 : 12580
Delete2 : 12453
Total : 100000
```

# Thread:100 WAL:true Pool:false Shared:true

```
TotalCount : 100000
TotalTime : 21128
TPS : 4732.8316531780965
Select1 : 12544
Select2 : 12461
Insert1 : 12463
Insert2 : 12560
Update1 : 12557
Update2 : 12549
Delete1 : 12463
Delete2 : 12403
Total : 100000
```

# Thread:100 WAL:true Pool:false Shared:false

```
TotalCount : 100000
TotalTime : 30355
TPS : 3294.241665568586
Select1 : 12343
Select2 : 12618
Insert1 : 12477
Insert2 : 12544
Update1 : 12548
Update2 : 12555
Delete1 : 12474
Delete2 : 12441
Total : 100000
```

# Thread:100 WAL:false Pool:true Shared:true

```
TotalCount : 100000
TotalTime : 78347
TPS : 1276.3567672435797
Select1 : 12680
Select2 : 12542
Insert1 : 12454
Insert2 : 12668
Update1 : 12573
Update2 : 12457
Delete1 : 12453
Delete2 : 12173
Total : 100000
```

# Thread:100 WAL:false Pool:true Shared:false (NG)

```
TotalCount : 100000
TotalTime : 84358
TPS : 1185.4240261741625
Select1 : 12038
Select2 : 12141
Insert1 : 12025
Insert2 : 12163
Update1 : 12142
Update2 : 12090
Delete1 : 12025
Delete2 : 12071
Total : 96695
```

# Thread:100 WAL:false Pool:false Shared:true

```
TotalCount : 100000
TotalTime : 81758
TPS : 1223.1069362394353
Select1 : 12426
Select2 : 12505
Insert1 : 12629
Insert2 : 12354
Update1 : 12512
Update2 : 12654
Delete1 : 12630
Delete2 : 12290
Total : 100000
```

# Thread:100 WAL:false Pool:false Shared:false (NG)

```
TotalCount : 100000
TotalTime : 99906
TPS : 1000.9408844313655
Select1 : 12054
Select2 : 12450
Insert1 : 12337
Insert2 : 12380
Update1 : 12380
Update2 : 12181
Delete1 : 12337
Delete2 : 12447
Total : 98566
```
