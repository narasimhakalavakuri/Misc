package com.positionsystem.repository;

import com.positionsystem.entity.Currency;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;
import java.util.List;

@Repository
public interface CurrencyRepository extends JpaRepository<Currency, String> {
    List<Currency> findByCodeContainingIgnoreCase(String code);
}