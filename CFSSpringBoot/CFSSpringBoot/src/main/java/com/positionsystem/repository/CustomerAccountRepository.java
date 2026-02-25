package com.positionsystem.repository;

import com.positionsystem.entity.CustomerAccount;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;
import java.util.List;
import java.util.Optional;

@Repository
public interface CustomerAccountRepository extends JpaRepository<CustomerAccount, String> {
    
    Optional<CustomerAccount> findByAccountNo(String accountNo);
    
    @Query("SELECT ca FROM CustomerAccount ca WHERE " +
           "ca.accountNo LIKE %:search% OR " +
           "ca.abbreviatedName LIKE %:search%")
    List<CustomerAccount> searchAccounts(@Param("search") String search);
}
