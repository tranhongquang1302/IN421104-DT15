#Requires -Version 7.2
[CmdletBinding()]
param(
    [uri]$BaseUrl = 'http://127.0.0.1:8080',
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9-]{0,11}$')]
    [string]$BatchCode = 'K01'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$apiBase = $BaseUrl.AbsoluteUri.TrimEnd('/')
$BatchCode = $BatchCode.ToUpperInvariant()
$password = $env:LANGCENTER_DEMO_PASSWORD
if ([string]::IsNullOrWhiteSpace($password)) {
    $password = Read-Host 'Demo password from Docker Compose' -MaskInput
}

function Invoke-DemoRequest {
    param([string]$Method, [string]$Path, [string]$Token, [object]$Body)

    $options = @{
        Method = $Method
        Uri = "$apiBase$Path"
        TimeoutSec = 15
    }
    if ($Token) {
        $options.Headers = @{ Authorization = "Bearer $Token" }
    }
    if ($null -ne $Body) {
        $options.ContentType = 'application/json'
        $options.Body = ConvertTo-Json -InputObject $Body -Compress
    }

    Invoke-RestMethod @options
}

$null = Invoke-DemoRequest -Method Get -Path '/healthz'
$registrar = (Invoke-DemoRequest -Method Post -Path '/api/auth/login' -Body @{
    email = 'registrar@langcenter.local'
    password = $password
}).accessToken
$cashier = (Invoke-DemoRequest -Method Post -Path '/api/auth/login' -Body @{
    email = 'cashier@langcenter.local'
    password = $password
}).accessToken

$sampleNames = @(
    'Nguyễn Khánh Linh', 'Trần Minh Khang', 'Lê Hoài An', 'Phạm Nhật Quang', 'Võ Ngọc Hân', 'Huỳnh Anh Thư', 'Đỗ Gia Bảo',
    'Bùi Minh Châu', 'Đặng Phương Uyên', 'Ngô Trung Kiên', 'Dương Thanh Trúc', 'Vũ Tuấn Kiệt', 'Phan Hải Yến', 'Lê Minh An',
    'Trịnh Hồng Nhung', 'Mai Khánh Duy', 'Tô Bảo Ngọc', 'Nguyễn Quốc Huy', 'Trần Khánh Vy', 'Cao Hoàng Nam', 'Hồ Mai Phương',
    'Vương Thanh Hà', 'Trần Mai Linh', 'Nguyễn Minh Anh', 'Đào Tuệ Nhi', 'Lý Đức Anh', 'Nguyễn Gia Huy', 'Phạm Thùy Dương',
    'Võ Hữu Phúc', 'Tạ Hoàng Yến', 'Lâm Ngọc Mai', 'Đinh Mạnh Hùng', 'Bùi Ngọc Khánh', 'Hoàng Lan Chi', 'Lê Thiện Nhân',
    'Đặng Ngọc Ánh', 'Nguyễn Khôi Nguyên', 'Lưu Như Quỳnh', 'Phạm Thu Hương', 'Ngô Bảo Trâm', 'Trương Hiếu Nghĩa', 'Vũ Thảo Nhi',
    'Mai Tuyết Nhi', 'Nguyễn Quỳnh Giang', 'Đỗ Hải Đăng', 'Võ Gia Hân', 'Hoàng Minh Nhật', 'Trần Tú Anh', 'Huỳnh Đức Thịnh',
    'Lê Nguyên Vũ', 'Phan Trúc Linh', 'Nguyễn Tường Vy', 'Đinh Minh Long', 'Hà Yến Nhi', 'Phạm Thanh Tâm', 'Cao Vân Anh',
    'Đặng Vĩnh Phúc', 'Đoàn Phương Thảo', 'Lý Minh Trí', 'Tô Nhật Hạ', 'Nguyễn Gia Khiêm', 'Đỗ Ngọc Bích', 'Bùi Tấn Phát',
    'Võ Kim Oanh', 'Vương Thiên Ân', 'Hồ Minh Tú', 'Trần Anh Dũng', 'Phạm Kiều Trang', 'Ngô Quỳnh Mai', 'Lê Hải Nam',
    'Dương Nhã Phương', 'Vũ Minh Đức', 'Huỳnh Ái Vy', 'Bùi Quốc Bảo', 'Lâm Khánh Ngân', 'Nguyễn Thiên Phúc', 'Đinh Mỹ Duyên',
    'Đỗ Thanh Tùng', 'Võ Tú Uyên', 'Đặng Hữu Tài', 'Cao Thu Ngân', 'Trương Minh Tâm', 'Phan Bảo Yến', 'Hoàng Nhật Minh'
)

# Returning learners reuse their student index across classes. Exam and certificate positions are within each roster.
$classPlans = @(
    @{
        code = 'A1'; name = 'Tiếng Anh A1 – Tối 2-4-6'; minimum = 36; fee = 2400000
        roster = @(0..12) + @(84..106); launch = $true; exams = @(1, 2, 5, 8)
    }
    @{
        code = 'N5'; name = 'Tiếng Nhật N5 – Tối 3-5-7'; minimum = 32; fee = 3200000
        roster = @(13..21) + @(107..128); launch = $false
    }
    @{
        code = 'B1'; name = 'Tiếng Anh B1 – Sáng cuối tuần'; minimum = 37; fee = 3600000
        roster = @(23..36) + @(129..151); launch = $true; exams = @(0, 2, 5, 8, 11); certificates = @(0, 2, 8)
    }
    @{
        code = 'A2'; name = 'Tiếng Anh A2 – Tối 3-5-7'; minimum = 35; fee = 2800000
        roster = @(37..47) + @(152..175); launch = $false
    }
    @{
        code = 'B2'; name = 'Tiếng Anh B2 – Tối 2-4-6'; minimum = 34; fee = 4200000
        roster = @(48..53) + @(176..200); launch = $false
    }
    @{
        code = 'N4'; name = 'Tiếng Nhật N4 – Sáng cuối tuần'; minimum = 33; fee = 3600000
        roster = @(54..65) + @(201..221); launch = $true; exams = @(1, 2, 5); certificates = @(5)
    }
    @{
        code = 'HSK1'; name = 'Tiếng Trung HSK 1 – Tối 3-5-7'; minimum = 34; fee = 2700000
        roster = @(66..73) + @(2, 7, 55, 59) + @(222..243); launch = $false
    }
    @{
        code = 'HSK2'; name = 'Tiếng Trung HSK 2 – Sáng cuối tuần'; minimum = 35; fee = 3100000
        roster = @(74..77) + @(40, 45, 47) + @(244..268); launch = $false
    }
    @{
        code = 'KO1'; name = 'Tiếng Hàn sơ cấp 1 – Tối 2-4-6'; minimum = 36; fee = 2900000
        roster = @(78..82) + @(13, 16, 19, 24, 32) + @(269..294); launch = $true
    }
    @{
        code = 'IELTS'; name = 'IELTS 5.5 – Tối 3-5-7'; minimum = 35; fee = 5400000
        roster = @(295..316) + @(24, 26, 28, 33, 34, 35, 36, 48, 49, 51, 53); launch = $false
    }
)

# Keep the scenario students stable and fill the larger rosters with distinct synthetic names.
$studentCount = ($classPlans | ForEach-Object { $_.roster } | Measure-Object -Maximum).Maximum + 1
$familyNames = @('Nguyễn', 'Trần', 'Lê', 'Phạm', 'Huỳnh', 'Võ', 'Đặng', 'Đỗ', 'Bùi', 'Hồ', 'Phan', 'Ngô', 'Lý', 'Vũ', 'Dương')
$givenNames = $sampleNames | ForEach-Object { ($_ -split ' ', 2)[1] } | Select-Object -Unique
$nameCandidates = foreach ($familyName in $familyNames) {
    foreach ($givenName in $givenNames) {
        "$familyName $givenName"
    }
}
$nameOrder = [Random]::new(421104)
$additionalNames = $nameCandidates | Where-Object { $_ -notin $sampleNames } |
    Sort-Object { $nameOrder.Next() } | Select-Object -First ($studentCount - $sampleNames.Count)
$studentNames = $sampleNames + $additionalNames

$emailPrefix = "langcenter-$($BatchCode.ToLowerInvariant())"
$existingStudents = Invoke-DemoRequest -Method Get -Path '/api/students' -Token $registrar
foreach ($existingStudent in $existingStudents) {
    if ($existingStudent.email -like "$emailPrefix-???@example.test") {
        throw "Demo batch $BatchCode already has student records. Use another -BatchCode to create a fresh cohort."
    }
}

# The unique first student email prevents two attempts to create the same batch in parallel.
$students = @(
    for ($index = 0; $index -lt $studentNames.Count; $index++) {
        $studentNumber = '{0:D3}' -f ($index + 1)
        Invoke-DemoRequest -Method Post -Path '/api/students' -Token $registrar -Body @{
            fullName = $studentNames[$index]
            email = "$emailPrefix-$studentNumber@example.test"
        }
    }
)

$classes = @{}
$rosters = @{}
$certificates = @{}
$enrollmentCount = 0
$paymentCount = 0
foreach ($plan in $classPlans) {
    Write-Verbose "Creating $($plan.name) for $BatchCode"
    $trainingClass = Invoke-DemoRequest -Method Post -Path '/api/classes' -Token $registrar -Body @{
        name = "$($plan.name) · $BatchCode"
        minimumEnrollment = $plan.minimum
        tuitionFee = $plan.fee
    }
    $classes[$plan.code] = $trainingClass
    $classId = $trainingClass.id
    $enrollments = @(
        foreach ($studentIndex in $plan.roster) {
            Invoke-DemoRequest -Method Post -Path "/api/classes/$classId/enrollments" -Token $registrar -Body @{
                studentId = $students[$studentIndex].id
            }
        }
    )
    $rosters[$plan.code] = $enrollments
    $enrollmentCount += $enrollments.Count

    if ($plan.launch) {
        $null = Invoke-DemoRequest -Method Post -Path "/api/classes/$classId/launch" -Token $registrar
    }

    for ($position = 0; $position -lt $enrollments.Count; $position++) {
        $enrollment = $enrollments[$position]
        $enrollmentId = $enrollment.id
        $tuitionDue = [decimal]$enrollment.tuitionDue
        $deposit = $tuitionDue / 2
        $paymentPattern = $position % 6
        $confirmExam = $plan['exams'] -contains $position
        $installments = @(
            if ($paymentPattern -eq 2 -and $plan.launch) {
                $deposit
                $tuitionDue - $deposit
            }
            elseif ($confirmExam -or $paymentPattern -eq 5 -or ($plan.launch -and $paymentPattern -eq 1)) {
                $tuitionDue
            }
            elseif ($paymentPattern -in 0, 4) {
                $deposit
            }
        )
        foreach ($amount in $installments) {
            $null = Invoke-DemoRequest -Method Post -Path "/api/enrollments/$enrollmentId/payments" -Token $cashier -Body @{
                amount = $amount
                requestId = [guid]::NewGuid()
            }
            $paymentCount++
        }

        if ($confirmExam) {
            $null = Invoke-DemoRequest -Method Post -Path "/api/enrollments/$enrollmentId/exam-admission" -Token $registrar
        }
        if ($plan['certificates'] -contains $position) {
            $certificates[$enrollmentId] = Invoke-DemoRequest -Method Post -Path "/api/enrollments/$enrollmentId/certificate" -Token $registrar -Body @{
                completionConfirmed = $true
            }
        }
    }
}

$partialEnrollment = $rosters['A1'][0]
$eligibleEnrollment = $rosters['A1'][1]
$underfilledClass = $classes['N5']
$waitingStudent = $students[22]
$certificateEnrollment = $rosters['B1'][0]
$certificate = $certificates[$certificateEnrollment.id]
$publicVerificationUrl = $certificate.verifyUrl -replace '/api/certificates/verify/([^/?#]+)$', '/certificates/verify/$1'

[pscustomobject]@{
    BatchCode = $BatchCode
    ClassesCreated = $classes.Count
    StudentsCreated = $students.Count
    EnrollmentsCreated = $enrollmentCount
    PaymentsRecorded = $paymentCount
    CertificatesIssued = $certificates.Count
    ClassId = $classes['A1'].id
    PartialEnrollmentId = $partialEnrollment.id
    PartialStudentEmail = $students[0].email
    PartialOutstandingAmount = $partialEnrollment.tuitionDue / 2
    EligibleEnrollmentId = $eligibleEnrollment.id
    EligibleStudentEmail = $students[1].email
    UnderfilledClassId = $underfilledClass.id
    UnderfilledEnrollmentId = $rosters['N5'][0].id
    UnderfilledEnrollmentCount = $rosters['N5'].Count
    UnderfilledMinimumEnrollment = $underfilledClass.minimumEnrollment
    WaitingStudentId = $waitingStudent.id
    WaitingStudentEmail = $waitingStudent.email
    CertificateClassId = $classes['B1'].id
    CertificateEnrollmentId = $certificateEnrollment.id
    CertificateStudentEmail = $students[23].email
    CertificateId = $certificate.id
    CertificateLookupCode = $certificate.lookupCode
    CertificateVerifyUrl = $publicVerificationUrl
}
