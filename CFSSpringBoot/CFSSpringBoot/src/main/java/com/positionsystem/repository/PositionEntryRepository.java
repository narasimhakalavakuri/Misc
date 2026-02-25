// package com.positionsystem.repository;

// import com.positionsystem.entity.EntryStatus;
// import com.positionsystem.entity.PositionEntry;
// import org.springframework.data.domain.Page;
// import org.springframework.data.domain.Pageable;
// import org.springframework.data.jpa.repository.JpaRepository;
// import org.springframework.data.jpa.repository.Query;
// import org.springframework.data.repository.query.Param;
// import org.springframework.stereotype.Repository;
// import java.time.LocalDate;
// import java.util.List;
// import java.util.Optional;

// @Repository
// public interface PositionEntryRepository extends JpaRepository<PositionEntry, Long> {
    
//     Optional<PositionEntry> findByUid(String uid);
    
//     Page<PositionEntry> findByStatusAndDepartment(
//         EntryStatus status, 
//         String department, 
//         Pageable pageable
//     );
    
//     List<PositionEntry> findByStatusAndDepartmentAndValueDate(
//         EntryStatus status,
//         String department,
//         LocalDate valueDate
//     );
    
//     List<PositionEntry> findByStatusAndDepartment(
//         EntryStatus status,
//         String department
//     );
    
//     @Query("SELECT COUNT(pe) > 0 FROM PositionEntry pe WHERE " +
//            "pe.department = :department AND " +
//            "pe.valueDate = :valueDate AND " +
//            "pe.inwardCurrency = :currency AND " +
//            "pe.inwardAmount = :amount AND " +
//            "pe.status != 'REJECTED'")
//     boolean existsDuplicateEntry(
//         @Param("department") String department,
//         @Param("valueDate") LocalDate valueDate,
//         @Param("currency") String currency,
//         @Param("amount") java.math.BigDecimal amount
//     );
// }


package com.positionsystem.repository;

import com.positionsystem.entity.PositionEntry;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface PositionEntryRepository extends JpaRepository<PositionEntry, Long> {
    Optional<PositionEntry> findByUid(String uid);
}
