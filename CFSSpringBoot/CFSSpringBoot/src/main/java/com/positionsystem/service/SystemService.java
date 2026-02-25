// package com.positionsystem.service;

// import com.positionsystem.entity.Department;
// import com.positionsystem.exception.ResourceNotFoundException;
// import com.positionsystem.exception.ValidationException;
// import com.positionsystem.repository.DepartmentRepository;
// import lombok.RequiredArgsConstructor;
// import lombok.extern.slf4j.Slf4j;
// import org.springframework.stereotype.Service;
// import org.springframework.transaction.annotation.Transactional;
// import java.time.LocalDate;
// import java.util.List;

// @Service
// @RequiredArgsConstructor
// @Slf4j
// public class SystemService {
    
//     private final DepartmentRepository departmentRepository;
    
//     public boolean isDepartmentClosed(String deptCode) {
//         return departmentRepository.findByCode(deptCode)
//             .map(dept -> Boolean.TRUE.equals(dept.getIsClosed()))
//             .orElse(false);
//     }
    
//     @Transactional
//     public void closeDay(String deptCode, String verifyDate) {
//         LocalDate currentDate = LocalDate.now();
//         LocalDate providedDate = LocalDate.parse(verifyDate);
        
//         if (!currentDate.equals(providedDate)) {
//             throw new ValidationException("Verify date must match current date");
//         }
        
//         Department dept = departmentRepository.findByCode(deptCode)
//             .orElseThrow(() -> new ResourceNotFoundException("Department not found: " + deptCode));
        
//         if (Boolean.TRUE.equals(dept.getIsClosed())) {
//             throw new ValidationException("Department is already closed");
//         }
        
//         dept.setIsClosed(true);
//         dept.setClosedDate(currentDate);
//         departmentRepository.save(dept);
        
//         log.info("Department {} closed for date {}", deptCode, currentDate);
//     }
    
//     @Transactional
//     public void reopenDay(String deptCode) {
//         Department dept = departmentRepository.findByCode(deptCode)
//             .orElseThrow(() -> new ResourceNotFoundException("Department not found: " + deptCode));
        
//         dept.setIsClosed(false);
//         dept.setClosedDate(null);
//         departmentRepository.save(dept);
        
//         log.info("Department {} reopened", deptCode);
//     }
    
//     public List<Department> getAllDepartments() {
//         return departmentRepository.findAll();
//     }
// }


package com.positionsystem.service;

import com.positionsystem.entity.Department;
import com.positionsystem.repository.DepartmentRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.time.LocalDate;
import java.util.List;

@Service
@RequiredArgsConstructor
public class SystemService {

    private final DepartmentRepository departmentRepository;

    public void closeDay(String department, String verifyDate) {
        // TODO: Implement close day logic
        System.out.println("Closing day for department: " + department + ", date: " + verifyDate);
    }

    public void reopenDay(String department) {
        // TODO: Implement reopen day logic
        System.out.println("Reopening day for department: " + department);
    }

    public List<Department> getAllDepartments() {
        return departmentRepository.findAll();
    }

    public boolean isDepartmentClosed(String department) {
        return departmentRepository.findById(department)
            .map(d -> d.getIsClosed() != null && d.getIsClosed())
            .orElse(false);
    }
}
